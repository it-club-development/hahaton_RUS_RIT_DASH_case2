import { useEffect, useRef, useState } from 'react';
import { clock, dateLabel, duration, lists } from '../../data';
import { downloadReport, loadIncidentPage, loadReportSummary } from '../../api';
import type { ReportSummary } from '../../api';
import type { Incident } from '../../data';
import './MainPage.css';

function IncidentDuration({ incident }: { incident: Incident }) {
  const [value, setValue] = useState(() => duration(incident));
  useEffect(() => {
    setValue(duration(incident));
    if (!incident.open) return;
    const timer = window.setInterval(() => setValue(duration(incident)), 1000);
    return () => window.clearInterval(timer);
  }, [incident]);
  return <>{clock(value)}</>;
}

const pageSize = 20;
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

function dayAfter(day: string) {
  if (day === '9999-12-31') return '9999-12-31T23:59:59.999999';
  const date = new Date(day + 'T00:00:00Z');
  date.setUTCDate(date.getUTCDate() + 1);
  return date.toISOString().slice(0, 10);
}

type ReportProps = { query: string | null; revision: number; onError: (error: unknown) => void };

const emptySummary: ReportSummary = { open: 0, closed: 0, skipped: 0, downtimeByDirection: {} };

function useSummary({ query, revision, onError }: ReportProps, period: string, day = '') {
  const errorHandler = useRef(onError);
  errorHandler.current = onError;
  const key = JSON.stringify([query, revision, period, day]);
  const [result, setResult] = useState<{ key: string; summary?: ReportSummary }>();

  useEffect(() => {
    if (!query) return;
    const params = new URLSearchParams(query);
    if (day) {
      const from = params.get('from')!;
      const to = params.get('to')!;
      const next = dayAfter(day);
      params.set('from', from > day ? from : day);
      params.set('to', to < next ? to : next);
    }
    if (params.get('from')! >= params.get('to')!) {
      setResult({ key, summary: emptySummary });
      return;
    }
    if (period) params.set('hours', period);
    let controller: AbortController | undefined;

    async function refresh() {
      if (controller) return;
      const pending = new AbortController();
      controller = pending;
      try {
        const summary = await loadReportSummary(params.toString(), pending.signal);
        if (!pending.signal.aborted) setResult({ key, summary });
      } catch (error) {
        if (!pending.signal.aborted) {
          setResult({ key });
          errorHandler.current(error);
        }
      } finally {
        controller = undefined;
      }
    }

    void refresh();
    const timer = window.setInterval(() => void refresh(), 30_000);
    return () => {
      window.clearInterval(timer);
      controller?.abort();
    };
  }, [key, query, period, day]);

  if (!query) return { summary: emptySummary, status: '' };
  if (result?.key !== key) return { status: 'Загрузка сводки…' };
  return { summary: result.summary, status: result.summary ? '' : 'Не удалось загрузить сводку.' };
}

function StatusChart(props: ReportProps) {
  const [period, setPeriod] = useState('');
  const canvas = useRef<HTMLCanvasElement>(null);
  const { summary, status } = useSummary(props, period);
  const open = summary?.open ?? 0;
  const total = open + (summary?.closed ?? 0);

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
    <p aria-live="polite">{status || (total ? 'Открытых аварий: ' + open : 'За выбранный период записей нет')}</p>
    <div className="chart-content" hidden={Boolean(status)} style={status ? { display: 'none' } : undefined}>
      <canvas ref={canvas} width="224" height="224" aria-hidden="true" />
      <ul>{[['Открытые', open], ['Закрытые', total - open]].map(([label, count], index) => <li key={label}>
        <span className="chart-key" style={{ backgroundColor: colors[index] }} aria-hidden="true" />{label} — {count}
      </li>)}</ul>
    </div>
  </section>;
}

function DowntimeSummary(props: ReportProps) {
  const [period, setPeriod] = useState('');
  const [day, setDay] = useState('');
  const { summary, status } = useSummary(props, period, day);
  const skipped = summary?.skipped ?? 0;
  const sorted = Object.entries(summary?.downtimeByDirection ?? {}).sort((a, b) => b[1] - a[1]);
  const total = sorted.reduce((sum, [, seconds]) => sum + seconds, 0);
  return <section className="panel report-chart">
    <h3>Суммарный простой БС по направлениям</h3>
    <PeriodSelect value={period} onChange={setPeriod} />
    <label className="downtime-day">Дата начала аварии<input type="date" value={day} onChange={event => setDay(event.target.value)} aria-describedby="downtime-day-help" /></label>
    <p id="downtime-day-help">Период и дата уточняют сводку по времени начала аварии.</p>
    <p aria-live="polite">{status || (sorted.length ? 'Всего (чч:мм:сс): ' + clock(total) : skipped ? 'Нет данных для расчёта' : 'За выбранный период аварий нет')}</p>
    <ul className="downtime-values" aria-label="Простой по направлениям">
      {sorted.map(([direction, seconds]) => <li key={direction}>{direction}<strong>{clock(seconds)}</strong></li>)}
    </ul>
    {skipped > 0 && <p>Записей без расчётного простоя: {skipped}</p>}
  </section>;
}

type Props = Omit<ReportProps, 'query'> & { canCreate: boolean; canManage: (incident: Incident) => boolean; onEdit: (incident: Incident | null) => void; onClose?: (incident: Incident) => void; onDelete: (incident: Incident) => void; deleting: boolean };
export default function MainPage({ revision, onError, canCreate, canManage, onEdit, onClose, onDelete, deleting }: Props) {
  const [filters, setFilters] = useState(initialFilters);
  const [sort, setSort] = useState<'desc' | 'asc'>('desc');
  const [page, setPage] = useState(1);
  const [mergedBoundaries, setMergedBoundaries] = useState<number[]>([]);
  const [result, setResult] = useState<{ query: string; key: string; rows: Incident[]; total: number }>();
  const errorHandler = useRef(onError);
  errorHandler.current = onError;

  function resetPagination() {
    setPage(1);
    setMergedBoundaries([]);
  }

  function setFilter(key: keyof typeof initialFilters, value: string) {
    setFilters(previous => ({ ...previous, [key]: value }));
    resetPagination();
  }
  const params = new URLSearchParams({
    from: filters.from || '1000-01-01',
    to: dayAfter(filters.to || '9999-12-31'),
    sort,
  });
  for (const field of ['direction', 'center', 'status'] as const) {
    if (filters[field]) params.set(field, filters[field]);
  }
  const query = filters.from && filters.to && filters.from > filters.to ? null : params.toString();
  const totalCount = result?.query === query ? result.total : 0;
  // Each available boundary marks the first row of a separate page.
  const pageStarts = [0];
  for (let index = pageSize; index < totalCount; index += pageSize) {
    if (!mergedBoundaries.includes(index)) pageStarts.push(index);
  }
  const totalPages = pageStarts.length;
  const currentPage = Math.min(page, totalPages);
  const offset = pageStarts[currentPage - 1];
  const nextOffset = pageStarts[currentPage];
  const hasMore = nextOffset !== undefined;
  const firstBatch = offset / pageSize + 1;
  // Each removed boundary expands the current logical page by one server page.
  let lastBatch = firstBatch;
  while (mergedBoundaries.includes(lastBatch * pageSize)) lastBatch++;
  const requestKey = [query, revision, firstBatch, lastBatch].join('|');
  const loaded = result?.key === requestKey;
  const visibleRows = loaded ? result.rows : [];
  const [failedRequest, setFailedRequest] = useState('');
  const failed = failedRequest === requestKey;

  useEffect(() => {
    if (!query) return;
    const controller = new AbortController();
    setFailedRequest('');
    const batches = Array.from({ length: lastBatch - firstBatch + 1 }, (_, index) =>
      loadIncidentPage(query, firstBatch + index, pageSize, controller.signal));
    Promise.all(batches).then(pages => {
      if (controller.signal.aborted) return;
      setResult({
        query, key: requestKey, total: pages[0].totalCount,
        rows: Array.from(new Map(([] as Incident[]).concat(...pages.map(value => value.items)).map(row => [row.id, row])).values()),
      });
    }).catch(error => {
      if (!controller.signal.aborted) {
        setFailedRequest(requestKey);
        errorHandler.current(error);
      }
    });
    return () => controller.abort();
  }, [query, firstBatch, lastBatch, requestKey]);

  return <section>
    <div className="heading">
      <div><h1>Отчётность</h1><p className="subtitle">Журнал аварий сотовой сети</p></div>
      <div className="heading-actions">
        <button className="btn" type="button" disabled={!query || !totalCount} onClick={() => { if (query) void downloadReport(query).catch(onError); }}>Скачать CSV</button>
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
      <label className="field">Дата начала<select value={sort} onChange={event => {
        setSort(event.target.value === 'asc' ? 'asc' : 'desc');
        resetPagination();
      }}>
        <option value="desc">Сначала новые ↓</option>
        <option value="asc">Сначала старые ↑</option>
      </select></label>
      <button className="btn" type="button" onClick={() => {
        setFilters(initialFilters);
        setSort('desc');
        resetPagination();
      }}>Сбросить</button>
    </form>
    <div className="panel">
      <div className="table-scroll"><table>
        <thead><tr>{['Начало', 'PW / NTT', 'Направление', 'ТЦ', 'Сетевой элемент', 'Длительность', 'Статус', 'Действие'].map(label => <th key={label} scope="col">{label}</th>)}</tr></thead>
        <tbody>{visibleRows.length ? visibleRows.map(row => <tr key={row.id}>
          <td>{dateLabel(row.start)}</td><td>{row.ticket}</td><td>{row.direction}</td><td>{row.center}</td><td>{row.element}</td><td><IncidentDuration incident={row} /></td>
          <td><span className={row.open ? 'status open' : 'status'}>{row.open ? 'Открыта' : 'Закрыта'}</span></td>
          <td>{canManage(row) ? <button type="button" className="link" aria-label={'Открыть ' + row.ticket} onClick={() => onEdit(row)}>Открыть</button> : <button type="button" className="link" aria-label={'Просмотреть ' + row.ticket} onClick={() => onEdit(row)}>Просмотреть</button>}{onClose && row.open && <button type="button" className="link incident-close" aria-label={'Закрыть аварию ' + row.ticket} disabled={deleting} onClick={() => onClose(row)}>Закрыть аварию</button>}{canManage(row) && <button type="button" className="link incident-delete" aria-label={'Удалить ' + row.ticket} disabled={deleting} onClick={() => onDelete(row)}>Удалить</button>}</td>
        </tr>) : <tr><td colSpan={8} className="empty">{!query ? 'Дата окончания раньше даты начала.' : failed ? 'Не удалось загрузить аварии.' : !loaded ? 'Загрузка…' : 'За выбранный период аварий нет.'}</td></tr>}</tbody>
      </table></div>
      <div className="foot report-pagination">
        <span role="status">{visibleRows.length
          ? 'Записи ' + (offset + 1) + '–' + (offset + visibleRows.length) + ' из ' + totalCount
          : query && !loaded && !failed ? 'Загрузка…' : 'Нет записей'}</span>
        {totalCount > pageSize && <div className="report-page-controls" role="group" aria-label="Страницы отчёта">
          <button className="btn" type="button" disabled={!loaded || currentPage === 1} onClick={() => setPage(currentPage - 1)}>Предыдущая</button>
          <label className="field report-page-select">Страница
            <select disabled={!loaded} value={currentPage} onChange={event => setPage(Number(event.target.value))}>
              {Array.from({ length: totalPages }, (_, index) => index + 1).map(number =>
                <option key={number} value={number}>{number} из {totalPages}</option>
              )}
            </select>
          </label>
          <button className="btn" type="button" disabled={!loaded || !hasMore} onClick={() => setPage(currentPage + 1)}>Следующая</button>
          {hasMore && <button className="btn primary" type="button" disabled={!loaded} onClick={() => {
            setPage(currentPage);
            setMergedBoundaries(previous => [...previous, nextOffset]);
          }}>Ещё</button>}
        </div>}
      </div>
    </div>
    <div className="report-charts"><StatusChart query={query} revision={revision} onError={onError} /><DowntimeSummary query={query} revision={revision} onError={onError} /></div>
  </section>;
}
