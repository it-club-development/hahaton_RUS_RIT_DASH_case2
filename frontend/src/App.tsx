import { useEffect, useState } from 'react';
import { ApiError, getProfile, loadIncidents, logout, saveIncident } from './api';
import type { Profile } from './api';
import type { Incident } from './data';
import LoginPage from './pages/LoginPage/LoginPage';
import MainPage from './pages/MainPage/MainPage';
import IncidentPage from './pages/IncidentPage/IncidentPage';

export default function App() {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [checking, setChecking] = useState(true);
  const [rows, setRows] = useState<Incident[]>([]);
  const [draft, setDraft] = useState<Incident | null | undefined>(undefined);
  const [notice, setNotice] = useState('');
  const [saving, setSaving] = useState(false);
  const [revision, reload] = useState(0);
  const admin = profile?.role === 'Admin';

  function changeProfile(next: Profile | null) {
    setProfile(next);
    setRows([]);
    setDraft(undefined);
    setNotice('');
  }

  function reportError(error: unknown) {
    if (error instanceof ApiError && error.status === 401) changeProfile(null);
    setNotice(error instanceof Error ? error.message : 'Не удалось выполнить запрос.');
  }

  useEffect(() => {
    async function restoreSession() {
      try {
        setProfile(await getProfile());
      } catch (error) {
        if (!(error instanceof ApiError && error.status === 401)) reportError(error);
      } finally {
        setChecking(false);
      }
    }

    void restoreSession();
  }, []);

  useEffect(() => {
    if (!profile) return;
    let active = true;
    loadIncidents().then(items => { if (active) setRows(items); })
      .catch(error => { if (active) reportError(error); });
    return () => { active = false; };
  }, [profile, revision]);

  function openIncident(value: Incident | null | undefined) {
    setDraft(value);
    setNotice('');
    window.scrollTo(0, 0);
  }

  async function submit(values: Omit<Incident, 'id'>) {
    if (!admin || draft === undefined || saving) return;
    setSaving(true);
    setNotice('');
    try {
      await saveIncident(values, draft?.id);
      openIncident(undefined);
      reload(current => current + 1);
      setNotice('Инцидент ' + values.ticket + ' сохранён.');
    } catch (error) {
      reportError(error);
    } finally {
      setSaving(false);
    }
  }

  async function signOut() {
    try { await logout(); changeProfile(null); }
    catch (error) { reportError(error); }
  }

  return <>
    <header>
      <div className="brand"><img className="brand-logo" src={import.meta.env.BASE_URL + 'motiv-logo.png'} alt="Веб-отчётность" /></div>
      {profile && <details className="profile" key={profile.id}>
        <summary className="btn">{profile.username}</summary>
        <div className="panel profile-card">
          <p>{admin ? 'Администратор' : 'Инженер'}</p>
          <button className="btn" type="button" onClick={signOut} disabled={saving}>Выйти</button>
        </div>
      </details>}
    </header>
    {profile ? <div className="shell">
      <nav aria-label="Разделы">
        <button type="button" disabled={saving} aria-current={draft === undefined ? 'page' : undefined} onClick={() => openIncident(undefined)}>Отчётность</button>
        {admin && <button type="button" disabled={saving} aria-current={draft !== undefined ? 'page' : undefined} onClick={() => { if (draft === undefined) openIncident(null); }}>Аварии</button>}
      </nav>
      <main>
        {notice && <p className="notice" role="status">{notice}</p>}
        <div hidden={draft !== undefined}><MainPage key={revision} incidents={rows} user={admin ? 'admin' : 'engineer'} onEdit={openIncident} /></div>
        {admin && draft !== undefined && <IncidentPage key={draft ? draft.id : 'new'} incident={draft} onSave={submit} saving={saving} onBack={() => openIncident(undefined)} />}
      </main>
    </div> : checking ? <p role="status">Проверяем сессию…</p> : <>
      {notice && <p className="notice" role="status">{notice}</p>}
      <LoginPage onLogin={changeProfile} />
    </>}
  </>;
}
