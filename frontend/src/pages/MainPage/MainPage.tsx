import { useEffect, useRef, useState } from 'react';
import { clock, dateLabel, downtime, duration, downloadCsv, filterByPeriod, lists } from '../../data';
import type { Incident } from '../../data';
import './MainPage.css';

const initialFilters = { from: '', to: '', direction: '', center: '', status: '' };
const periods = [['', 'Не выбрано'], ['1', '1 ч'], ['12', '12 ч'], ['24', '1 д'], ['168', '7 д'], ['720', '30 д']];
const colors = ['#215baa', '#b14d0c'];

function PeriodSelect({ value, onChange }: { value: string; onChange: (value: string) => void }) {
  return <label className="report-period">Период
    <select value={value} onChange={event => onChange(event.target.value)}>
      {periods.map(([key, label]) => <option key={key} value={key}>{label}</option>)}
    </select>
  </label>;
}

function StatusChart({ rows }: { rows: Incident[] }) {
  const [period, setPeriod] = useState('');
  const canvas = useRef<HTMLCanvasElement>(null);
  const filtered = filterByPeriod(rows, period);
  const total = filtered.length;
  const open = filtered.filter(row => row.open).length;

  useEffect(() => {
    const context = canvas.current?.getContext('2d');
    if (!context) return;
    context.clearRect(0, 0, 224, 224);
    let angle = -Math.PI / 2;
    const slices = total ? [open / total, (total - open) / total] : [1];
    slices.forEach((fraction, index) => {
      if (!fraction) return;
      const end = angle + fraction * 2 * Math.PI;
      context.beginPath();
      context.moveTo(112, 112);
      context.arc(112, 112, 109, angle, end);
      context.closePath();
      context.fillStyle = total ? colors[index] : '#e1e8f2';
      context.fill();
      angle = end;
    });
  }, [open, total]);

  return <section className="panel report-chart">
    <h3>Аварии по статусу</h3>
    <PeriodSelect value={period} onChange={setPeriod} />
    <p aria-live="polite">{total ? 'Открытых аварий: ' + open : 'За выбранный период записей нет'}</p>
    <div className="chart-content">
      <canvas ref={canvas} width="224" height="224" aria-hidden="true" />
      <ul>{[['Открытые', open], ['Закрытые', total - open]].map(([label, count], index) => <li key={label}>
        <span className="chart-key" style={{ backgroundColor: colors[index] }} aria-hidden="true" />{label} — {count}
      </li>)}</ul>
    </div>
  </section>;
}

function DowntimeSummary({ rows }: { rows: Incident[] }) {
  const [period, setPeriod] = useState('');
  const [day, setDay] = useState('');
  const selected = filterByPeriod(rows, period).filter(row => !day || row.start.slice(0, 10) === day);
  const values = new Map<string, number>();
  let skipped = 0;
  let total = 0;
  for (const row of selected) {
    const seconds = downtime(row);
    if (seconds === null || !Number.isFinite(seconds)) {
      skipped++;
      continue;
    }
    const direction = row.direction || 'Без направления';
    values.set(direction, (values.get(direction) || 0) + seconds);
    total += seconds;
  }
  const sorted = Array.from(values).sort((a, b) => b[1] - a[1]);
  return <section className="panel report-chart">
    <h3>Суммарный простой БС по направлениям</h3>
    <PeriodSelect value={period} onChange={setPeriod} />
    <label className="downtime-day">Дата начала аварии<input type="date" value={day} onChange={event => setDay(event.target.value)} aria-describedby="downtime-day-help" /></label>
    <p id="downtime-day-help">Период и дата фильтруют записи таблицы по времени начала аварии.</p>
    <p aria-live="polite">{sorted.length ? 'Всего (чч:мм:сс): ' + clock(total) : selected.length ? 'Нет данных для расчёта' : 'За выбранный период аварий нет'}</p>
    <ul className="downtime-values" aria-label="Простой по направлениям">
      {sorted.map(([direction, seconds]) => <li key={direction}>{direction}<strong>{clock(seconds)}</strong></li>)}
    </ul>
    {skipped > 0 && <p>Записей без расчётного простоя: {skipped}</p>}
  </section>;
}

type Props = { incidents: Incident[]; canCreate: boolean; canManage: (incident: Incident) => boolean; onEdit: (incident: Incident | null) => void; onDelete: (incident: Incident) => void; deleting: boolean };
export default function MainPage({ incidents, canCreate, canManage, onEdit, onDelete, deleting }: Props) {
  const [filters, setFilters] = useState(initialFilters);
  function setFilter(key: keyof typeof initialFilters, value: string) {
    setFilters(previous => ({ ...previous, [key]: value }));
  }
  const rows = incidents.filter(row => {
    const day = row.start.slice(0, 10);
    return (!filters.from || day >= filters.from) && (!filters.to || day <= filters.to) &&
      (!filters.direction || row.direction === filters.direction) &&
      (!filters.center || row.center === filters.center) &&
      (!filters.status || (filters.status === 'open') === row.open);
  }).sort((a, b) => b.start.localeCompare(a.start));

  return <section>
    <div className="heading">
      <div><h1>Отчётность</h1><p className="subtitle">Журнал аварий сотовой сети</p></div>
      <div className="heading-actions">
        <button className="btn" type="button" disabled={!rows.length} onClick={() => downloadCsv(rows)}>Скачать CSV</button>
        {canCreate && <button className="btn primary" type="button" onClick={() => onEdit(null)}>Новый инцидент</button>}
      </div>
    </div>
    <form className="filters panel" aria-label="Фильтры отчёта" onSubmit={event => event.preventDefault()}>
      <label className="field">Начало периода<input type="date" value={filters.from} onChange={event => setFilter('from', event.target.value)} /></label>
      <label className="field">Конец периода<input type="date" value={filters.to} onChange={event => setFilter('to', event.target.value)} /></label>
      {(['direction', 'center'] as const).map(key => <label className="field" key={key}>{key === 'direction' ? 'Направление' : 'Технический центр'}
        <select value={filters[key]} onChange={event => setFilter(key, event.target.value)}><option value="">Все</option>{lists[key].map(item => <option key={item}>{item}</option>)}</select>
      </label>)}
      <label className="field">Статус<select value={filters.status} onChange={event => setFilter('status', event.target.value)}>
        <option value="">Все</option><option value="open">Открыта</option><option value="closed">Закрыта</option>
      </select></label>
      <button className="btn" type="button" onClick={() => setFilters(initialFilters)}>Сбросить</button>
    </form>
    <div className="panel">
      <div className="table-scroll"><table>
        <thead><tr>{['Начало', 'PW / NTT', 'Направление', 'ТЦ', 'Сетевой элемент', 'Длительность', 'Статус', 'Действие'].map(label => <th key={label} scope="col">{label}</th>)}</tr></thead>
        <tbody>{rows.length ? rows.map(row => <tr key={row.id}>
          <td>{dateLabel(row.start)}</td><td>{row.ticket}</td><td>{row.direction}</td><td>{row.center}</td><td>{row.element}</td><td>{clock(duration(row))}</td>
          <td><span className={row.open ? 'status open' : 'status'}>{row.open ? 'Открыта' : 'Закрыта'}</span></td>
          <td>{canManage(row) ? <button type="button" className="link" aria-label={'Открыть ' + row.ticket} onClick={() => onEdit(row)}>Открыть</button> : '—'}{canManage(row) && <button type="button" className="link incident-delete" aria-label={'Удалить ' + row.ticket} disabled={deleting} onClick={() => onDelete(row)}>Удалить</button>}</td>
        </tr>) : <tr><td colSpan={8} className="empty">За выбранный период аварий нет.</td></tr>}</tbody>
      </table></div>
      <div className="foot">Показано записей: {rows.length} из {incidents.length}</div>
    </div>
    <div className="report-charts"><StatusChart rows={rows} /><DowntimeSummary rows={rows} /></div>
  </section>;
}
