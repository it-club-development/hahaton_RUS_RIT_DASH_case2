import { useState } from 'react';
import type { FormEvent } from 'react';
import { isUser } from '../../data';
import type { User } from '../../data';
import './LoginPage.css';

export default function LoginPage({ onLogin }: { onLogin: (user: User) => void }) {
  const [error, setError] = useState(false);
  function login(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const name = String(data.get('username')).trim().toLowerCase();
    if (!isUser(name) || data.get('password') !== 'demo') {
      setError(true);
      return;
    }
    onLogin(name);
  }
  return <section className="panel login">
    <h1>Вход</h1><p className="subtitle">Демонстрационный режим</p>
    <form onSubmit={login}>
      <label className="field">Логин<input name="username" autoComplete="username" required /></label>
      <label className="field">Пароль<input name="password" type="password" autoComplete="current-password" required /></label>
      {error && <p className="notice" role="alert">Неверный логин или пароль.</p>}
      <button className="btn primary" type="submit">Войти</button>
    </form>
    <p className="summary">Логины: admin, duty, engineer. Пароль для всех: demo.</p>
  </section>;
}
