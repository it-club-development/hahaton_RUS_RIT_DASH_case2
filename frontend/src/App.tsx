import { useState } from 'react';
import { accounts, canEdit, isUser, loadIncidents, sessionKey, storageKey } from './data';
import type { Incident, User } from './data';
import LoginPage from './pages/LoginPage/LoginPage';
import MainPage from './pages/MainPage/MainPage';
import IncidentPage from './pages/IncidentPage/IncidentPage';

export default function App() {
  const [user, setUser] = useState<User | null>(() => {
    try {
      const saved = sessionStorage.getItem(sessionKey);
      return isUser(saved) ? saved : null;
    } catch { return null; }
  });
  const [incidents, setIncidents] = useState(loadIncidents);
  const [draft, setDraft] = useState<Incident | null | undefined>(undefined);
  const [notice, setNotice] = useState('');
  const [reportKey, resetReport] = useState(0);

  function changeUser(next: User | null) {
    try {
      if (next) sessionStorage.setItem(sessionKey, next);
      else sessionStorage.removeItem(sessionKey);
    } catch { /* The demo session can still run in memory. */ }
    setUser(next);
    setDraft(undefined);
    setNotice('');
    resetReport(key => key + 1);
  }

  function openIncident(incident: Incident | null) {
    if (!user || user === 'engineer' || (incident && !canEdit(user, incident))) return;
    setNotice('');
    setDraft(incident);
    window.scrollTo(0, 0);
  }

  function saveIncident(values: Omit<Incident, 'id'>) {
    if (!user || user === 'engineer' || draft === undefined) return;
    const existing = draft && incidents.find(row => row.id === draft.id);
    if (draft && (!existing || !canEdit(user, existing))) return;
    const record: Incident = {
      ...values,
      id: existing ? existing.id : 'incident-' + Date.now() + '-' + Math.random().toString(36).slice(2, 8),
      createdBy: existing ? existing.createdBy : user,
    };
    const next = existing ? incidents.map(row => row.id === existing.id ? record : row) : [...incidents, record];
    try {
      localStorage.setItem(storageKey, JSON.stringify(next));
    } catch {
      setNotice('Браузер не смог сохранить данные.');
      return;
    }
    setIncidents(next);
    setDraft(undefined);
    resetReport(key => key + 1);
    setNotice('Инцидент ' + record.ticket + ' сохранён.');
    window.scrollTo(0, 0);
  }

  function showReport() {
    setDraft(undefined);
    setNotice('');
    window.scrollTo(0, 0);
  }

  return <>
    <header>
      <div className="brand"><img className="brand-logo" src={import.meta.env.BASE_URL + 'motiv-logo.png'} alt="Веб-отчётность" /></div>
      {user && <details className="profile" key={user}>
        <summary className="btn">{accounts[user]}</summary>
        <div className="panel profile-card"><strong>{accounts[user]}</strong><p>{user === 'duty' ? 'Дежурный' : accounts[user]}</p><button className="btn" type="button" onClick={() => changeUser(null)}>Выйти</button></div>
      </details>}
    </header>
    {user ? <div className="shell">
      <nav aria-label="Разделы">
        <button type="button" aria-current={draft === undefined ? 'page' : undefined} onClick={showReport}><span aria-hidden="true">▤</span>Отчётность</button>
        {user !== 'engineer' && <button type="button" aria-current={draft !== undefined ? 'page' : undefined} onClick={() => { if (draft === undefined) openIncident(null); }}><span aria-hidden="true">＋</span>Аварии</button>}
      </nav>
      <main>
        {notice && <p className="notice" role="status">{notice}</p>}
        <div hidden={draft !== undefined}><MainPage key={reportKey} incidents={incidents} user={user} onEdit={openIncident} /></div>
        {draft !== undefined && <IncidentPage key={draft ? draft.id : 'new'} incident={draft} onSave={saveIncident} onBack={showReport} />}
      </main>
    </div> : <LoginPage onLogin={changeUser} />}
  </>;
}
