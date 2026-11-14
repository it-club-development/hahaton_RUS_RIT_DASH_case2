import { useState } from 'react';
import type { FormEvent } from 'react';
import { clock, downtime, duration, lists, localDate } from '../../data';
import type { Field, Incident } from '../../data';
import './IncidentPage.css';

type Values = Omit<Incident, 'id'>;
type Props = { incident: Incident | null; onSave: (record: Values) => void; onBack: () => void; saving: boolean; readOnly: boolean; showStatusActions: boolean };
const timePattern = '[0-9]{1,3}:[0-5][0-9]:[0-5][0-9]';
const stationPattern = '(?:(?=[0-9]{1,4}$)0*[1-9][0-9]*|10000|[Сс])';

export default function IncidentPage({ incident, onSave, onBack, saving, readOnly, showStatusActions }: Props) {
  const [draft, setDraft] = useState<Values>(() => {
    if (!incident) return { start: localDate(new Date()), open: true };
    return { ...incident };
  });
  function setField(field: Field, value: string) {
    setDraft(previous => ({ ...previous, [field]: value }));
  }
  function input(field: Field, label: string, { wide = false, ...props }: { wide?: boolean; required?: boolean; maxLength?: number; placeholder?: string; pattern?: string; title?: string; type?: string; step?: string; disabled?: boolean; min?: string } = {}) {
    return <label className={wide ? 'field wide' : 'field'}>{label}
      <input {...props} name={field} value={draft[field] || ''} onChange={event => setField(field, event.target.value)} />
    </label>;
  }
  function select(field: keyof typeof lists, label: string) {
    return <label className="field">{label}
      <select name={field} required value={draft[field] || ''} onChange={event => setField(field, event.target.value)}>
        <option value="">Выберите значение</option>{lists[field].map(item => <option key={item}>{item}</option>)}
      </select>
    </label>;
  }
  function textarea(field: Field, label: string, wide = false) {
    return <label className={wide ? 'field wide' : 'field'}>{label}
      <textarea name={field} rows={wide ? 3 : 2} maxLength={450} value={draft[field] || ''} onChange={event => setField(field, event.target.value)} />
    </label>;
  }
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (readOnly || saving) return;
    const values = { ...draft };
    for (const [key, value] of Object.entries(values)) {
      if (typeof value === 'string') Object.assign(values, { [key]: value.trim() });
    }
    if (values.open) values.end = '';
    onSave(values);
  }
  return <section className="incident-page">
    <div className="heading"><div><h1>{incident ? 'Инцидент ' + incident.ticket : 'Новый инцидент'}</h1><p className="subtitle">Карточка аварии базовой станции или сетевого элемента</p></div><button className="btn" type="button" onClick={onBack} disabled={saving}>К отчётности</button></div>
    <form onSubmit={submit}>
      <fieldset className="incident-fields" disabled={readOnly || saving}>
      <div className="panel form-section"><h2>Основные сведения</h2><div className="grid">
        {input('ticket', 'Номер или ссылка PW / NTT', { maxLength: 450, required: true })}
        {select('direction', 'Направление')}{select('center', 'Технический центр')}{select('category', 'Категория')}
        {select('network', 'Тип сети')}{select('type', 'Тип аварии')}
        {input('element', 'Сетевой элемент, услуга', { maxLength: 450, required: true })}
        {input('stations', 'Количество БС или «С» для сектора', { maxLength: 5, required: true, pattern: stationPattern, title: 'Число от 1 до 10000 или буква «С»' })}
        {input('equipment', 'Тип оборудования', { maxLength: 450, wide: true })}
      </div></div>
      <div className="panel form-section"><h2>Время и простой</h2>
        {showStatusActions && incident && !readOnly && <div className="actions incident-status-actions">
          {draft.open ? <button className="btn" type="button" onClick={() => setDraft(previous => ({ ...previous, open: false, end: localDate(new Date()) }))}>Закрыть аварию</button>
            : <button className="btn" type="button" onClick={() => setDraft(previous => ({ ...previous, open: true, end: '' }))}>Открыть снова</button>}
          {draft.open !== incident.open && <span role="status">Чтобы применить изменение, сохрани аварию.</span>}
        </div>}
        <div className="grid">
        {input('start', 'Начало', { type: 'datetime-local', step: '1', required: true })}
        {input('end', 'Окончание', { type: 'datetime-local', step: '1', min: draft.start, required: !draft.open, disabled: draft.open })}
        <label className="field check"><input type="checkbox" checked={draft.open} onChange={event => setDraft({ ...draft, open: event.target.checked })} />Авария продолжается (НВ)</label>
        {input('interruption', 'Среднее время прерыва связи', { pattern: timePattern, placeholder: 'чч:мм:сс' })}
        {input('battery', 'Время работы от АКБ', { pattern: timePattern, placeholder: 'чч:мм:сс' })}
        {input('generator', 'Время работы от генератора', { pattern: timePattern, placeholder: 'чч:мм:сс' })}
      </div><p className="summary" aria-live="polite">Длительность: {clock(duration(draft))} · Общий простой: {clock(downtime(draft))}</p></div>
      <div className="panel form-section"><h2>Описание и уведомления</h2><div className="grid">
        {textarea('description', 'Краткое описание')}{textarea('notified', 'Кого уведомили')}{textarea('details', 'Подробное описание', true)}
      </div></div>
      </fieldset>
      {!readOnly && <div className="actions"><button className="btn primary" type="submit" disabled={saving}>Сохранить инцидент</button><button className="btn" type="button" onClick={onBack} disabled={saving}>Отмена</button></div>}
    </form>
  </section>;
}
