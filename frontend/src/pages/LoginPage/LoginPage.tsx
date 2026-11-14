import { useState } from 'react';
import type { FormEvent } from 'react';
import { login } from '../../api';
import type { Profile } from '../../api';
import './LoginPage.css';

export default function LoginPage({ onLogin }: { onLogin: (profile: Profile) => void }) {
  const [error, setError] = useState('');
  const [pending, setPending] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending) return;
    const data = new FormData(event.currentTarget);
    setError('');
    setPending(true);
    try {
      onLogin(await login(String(data.get('username')).trim(), String(data.get('password'))));
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Ошибка входа.');
    } finally {
      setPending(false);
    }
  }
  return <section className="panel login">
    <h1>Вход</h1>
    <form onSubmit={submit}>
      <label className="field">Логин<input name="username" autoComplete="username" required /></label>
      <label className="field">Пароль<input name="password" type="password" autoComplete="current-password" required /></label>
      {error && <p className="notice" role="alert">{error}</p>}
      <button className="btn primary" type="submit" disabled={pending}>Войти</button>
    </form>
  </section>;
}
