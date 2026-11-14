import { useEffect, useState } from 'react';
import { ApiError, getProfile, loadIncidents, logout, saveIncident, deleteIncident } from './api';
import type { Profile } from './api';
import type { Incident } from './data';
import LoginPage from './pages/LoginPage/LoginPage';
import MainPage from './pages/MainPage/MainPage';
import ThemePicker from './ThemePicker';
import UsersPage from './pages/UsersPage';
import IncidentPage from './pages/IncidentPage/IncidentPage';

export default function App() {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [checking, setChecking] = useState(true);
  const [rows, setRows] = useState<Incident[]>([]);
  const [draft, setDraft] = useState<Incident | null | undefined>(undefined);
  const [notice, setNotice] = useState('');
  const [saving, setSaving] = useState(false);
  const [revision, reload] = useState(0);
  const [settings, setSettings] = useState<'users' | 'password' | null>(null);
  const admin = profile?.role === 'Admin';

  const canCreate = admin || profile?.role === 'Attendant';

  function canManage(incident: Incident) {
    return admin || (profile?.role === 'Attendant' && incident.createdByUserId === profile.id);
  }

  function changeProfile(next: Profile | null) {
    setSettings(null);
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
    setSettings(null);
    setDraft(value);
    setNotice('');
    window.scrollTo(0, 0);
  }

  async function submit(values: Omit<Incident, 'id'>) {
    if (!canCreate || draft === undefined || saving || (draft && !canManage(draft))) return;
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

  async function removeIncident(incident: Incident) {
    if (!canManage(incident) || saving) return;
    if (!window.confirm('Удалить аварию «' + (incident.ticket || incident.id) + '»? Действие нельзя отменить.')) return;

    setSaving(true);
    setNotice('');
    try {
      await deleteIncident(incident.id);
      setRows(current => current.filter(row => row.id !== incident.id));
      setNotice('Авария удалена.');
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
          <p>{admin ? 'Администратор' : profile.role === 'Attendant' ? 'Дежурный' : 'Инженер'}</p>
          <button className="btn" type="button" onClick={() => { setNotice(""); setSettings("password"); }} disabled={saving}>Сменить пароль</button>
          <button className="btn" type="button" onClick={signOut} disabled={saving}>Выйти</button>
        </div>
      </details>}
    </header>
    <ThemePicker />
    {profile ? <div className="shell">
      <nav aria-label="Разделы">
        <button type="button" disabled={saving} aria-current={!settings && draft === undefined ? 'page' : undefined} onClick={() => openIncident(undefined)}>Отчётность</button>
        {canCreate && <button type="button" disabled={saving} aria-current={!settings && draft !== undefined ? 'page' : undefined} onClick={() => { if (settings || draft === undefined) openIncident(null); }}>Аварии</button>}
        {admin && <button type="button" disabled={saving} aria-current={settings === "users" ? "page" : undefined} onClick={() => { setNotice(""); setSettings("users"); }}>Пользователи</button>}
      </nav>
      <main>
        {notice && <p className="notice" role="status">{notice}</p>}
        <div hidden={Boolean(settings) || draft !== undefined}><MainPage key={revision} incidents={rows} canCreate={canCreate} canManage={canManage} onEdit={openIncident} onDelete={removeIncident} deleting={saving} /></div>
        {settings && <UsersPage key={settings} profile={profile} mode={settings} onExpired={() => { changeProfile(null); setNotice("Сессия завершилась. Войди снова."); }} onChanged={() => { changeProfile(null); setNotice("Пароль изменён. Войди с новым паролем."); }} />}
        <div hidden={Boolean(settings)}>{canCreate && draft !== undefined && <IncidentPage key={draft ? draft.id : 'new'} incident={draft} onSave={submit} saving={saving} onBack={() => openIncident(undefined)} />}</div>
      </main>
    </div> : checking ? <p role="status">Проверяем сессию…</p> : <>
      {notice && <p className="notice" role="status">{notice}</p>}
      <LoginPage onLogin={changeProfile} />
    </>}
  </>;
}
