import { useEffect, useState } from 'react';
import './ThemePicker.css';

const themes = [['light', 'Светлая'], ['dark', 'Тёмная'], ['gray', 'Серая'], ['custom', 'Кастомная']];
const key = 'motiv-theme';

function readSettings() {
  const defaults = { theme: 'light', color: '#3158d9' };
  try {
    const saved = JSON.parse(localStorage.getItem(key) || 'null');
    return {
      theme: themes.some(([theme]) => theme === saved?.theme) ? String(saved.theme) : defaults.theme,
      color: /^#[0-9a-f]{6}$/i.test(saved?.color) ? String(saved.color) : defaults.color,
    };
  } catch { return defaults; }
}

export default function ThemePicker() {
  const [settings, setSettings] = useState(readSettings);
  useEffect(() => {
    const root = document.documentElement;
    root.dataset.theme = settings.theme;
    const rgb = [1, 3, 5].map(index => parseInt(settings.color.slice(index, index + 2), 16));
    const [r, g, b] = rgb.map(channel => {
      const value = channel / 255;
      return value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
    });
    root.style.setProperty('--custom-accent', settings.color);
    root.style.setProperty('--custom-rgb', rgb.join(', '));
    root.style.setProperty('--custom-on-accent', r * 0.2126 + g * 0.7152 + b * 0.0722 > 0.179 ? '#000' : '#fff');
    try { localStorage.setItem(key, JSON.stringify(settings)); }
    catch { /* Theme selection can also work without storage. */ }
  }, [settings]);

  return <details className="theme-picker">
    <summary className="btn">Тема</summary>
    <div className="panel theme-options">
      <label className="field">Цветовая тема
        <select value={settings.theme} onChange={event => setSettings({ ...settings, theme: event.target.value })}>
          {themes.map(([theme, label]) => <option key={theme} value={theme}>{label}</option>)}
        </select>
      </label>
      {settings.theme === 'custom' && <label className="field">Цвет оформления
        <input type="color" value={settings.color} onChange={event => setSettings({ ...settings, color: event.target.value })} />
      </label>}
    </div>
  </details>;
}
