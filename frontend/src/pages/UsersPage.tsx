import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { changePassword, createUser, listUsers, resetPassword } from '../api';
import type { Profile } from '../api';
import './UsersPage.css';

type Props = {
  profile: Profile;
  mode: 'users' | 'password';
  onChanged: () => void;
  onExpired: () => void;
};

export default function UsersPage({ profile, mode, onChanged, onExpired }: Props) {
  const [users, setUsers] = useState<Profile[]>([]);
  const [loading, setLoading] = useState(mode === 'users');
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [selected, setSelected] = useState('');

  useEffect(() => {
    if (mode !== 'users') return;
    let active = true;
    async function load() {
      try {
        const list = await listUsers();
        if (active) setUsers(list);
      } catch (error) {
        if (active) showError(error);
      } finally {
        if (active) setLoading(false);
      }
    }
    void load();
    return () => { active = false; };
  }, [mode]);

  function showError(error: unknown) {
    if (error instanceof Error && 'status' in error && error.status === 401) {
      onExpired();
      return;
    }
    setError(error instanceof Error ? error.message : 'Операцию выполнить не удалось.');
  }

  async function submit(event: FormEvent<HTMLFormElement>, operation: 'create' | 'reset' | 'change') {
    event.preventDefault();
    if (pending) return;
    const form = event.currentTarget;
    const data = new FormData(form);
    const password = String(data.get('password') || '');
    setMessage('');
    setError('');
    if (password !== data.get('confirm')) {
      setError('Пароли не совпадают.');
      return;
    }
    setPending(true);
    try {
      if (operation === 'create') {
        const user = await createUser(String(data.get('username')).trim(), password, data.get('role') === 'Admin' ? 'Admin' : data.get('role') === 'Attendant' ? 'Attendant' : 'Engineer');
        setUsers(list => [...list, user]);
        setMessage('Пользователь «' + user.username + '» создан.');
      } else if (operation === 'reset') {
        await resetPassword(selected, password);
        setMessage('Новый пароль установлен.');
        setSelected('');
      } else {
        await changePassword(String(data.get('currentPassword')), password);
        onChanged();
      }
      form.reset();
    } catch (error) {
      showError(error);
    } finally {
      setPending(false);
    }
  }

  const passwordFields = <>
    <label className="field">Новый пароль<input name="password" type="password" autoComplete="new-password" minLength={12} maxLength={128} required /></label>
    <label className="field">Повтори новый пароль<input name="confirm" type="password" autoComplete="new-password" minLength={12} maxLength={128} required /></label>
  </>;

  return <section className="users-page">
    <h1>{mode === 'users' ? 'Пользователи' : 'Смена пароля'}</h1>
    <p className="subtitle">Пароль должен содержать от 12 до 128 символов.</p>
    {error && <p className="users-error" role="alert">{error}</p>}
    {message && <p role="status">{message}</p>}
    {mode === 'password' ? <form className="panel users-form" onSubmit={event => void submit(event, 'change')}>
      <label className="field">Текущий пароль<input name="currentPassword" type="password" autoComplete="current-password" required /></label>
      {passwordFields}
      <button className="btn primary" disabled={pending}>Сменить пароль</button>
    </form> : <>
      <form className="panel users-form" onSubmit={event => void submit(event, 'create')}>
        <h2>Создать учётную запись</h2>
        <label className="field">Логин<input name="username" autoComplete="off" minLength={3} maxLength={100} required /></label>
        <label className="field">Роль<select name="role" defaultValue="Engineer"><option value="Engineer">Инженер</option><option value="Attendant">Дежурный</option><option value="Admin">Администратор</option></select></label>
        {passwordFields}
        <button className="btn primary" disabled={pending}>Создать пользователя</button>
      </form>
      <form className="panel users-form" onSubmit={event => void submit(event, 'reset')}>
        <h2>Сброс пароля пользователя</h2>
        <label className="field">Учётная запись<select required value={selected} disabled={loading} onChange={event => setSelected(event.target.value)}>
          <option value="">{loading ? 'Загрузка…' : 'Выбери пользователя'}</option>
          {users.filter(user => user.id !== profile.id).map(user => <option key={user.id} value={user.id}>{user.username}</option>)}
        </select></label>
        {passwordFields}
        <button className="btn" disabled={pending || !selected}>Установить новый пароль</button>
      </form>
    </>}
  </section>;
}

