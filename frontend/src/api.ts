import { setBusinessTimeZone } from './data';
import type { Incident } from './data';

export type Profile = { id: string; username: string; role: 'Admin' | 'Engineer' | 'Attendant' };
export const roleLabels: Record<Profile['role'], string> = {
  Admin: 'Администратор',
  Attendant: 'Дежурный',
  Engineer: 'Инженер',
};

type ServerIncident = {
  id: string; version: string; createdByUserId: string | null; pw: string; direction: string; techCenter: string; category: number;
  networkType: string; serviceElement: string; numberOfBaseStations: string;
  equipmentType: string; alarmType: string; description: string; beginTime: string;
  endTime: string | null; akb: string | null; generatorTime: string | null;
  baseStationTime: string | null; alarmWho: string; noticeDescription: string;
};

export class ApiError extends Error {
  readonly status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

async function request(path: string, options?: RequestInit) {
  const response = await fetch('/api/' + path, { ...options, credentials: 'same-origin', cache: 'no-store' });
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    const errors = problem?.errors && Object.values(problem.errors).join(' ');
    throw new ApiError(response.status, errors || problem?.detail || problem?.title || (
      response.status === 401 ? 'Войдите в систему.' : 'Ошибка сервера: ' + response.status
    ));
  }
  return response;
}

async function write(path: string, method: string, payload?: object) {
  const token = await (await request('account/antiforgery', { cache: 'no-store' })).json() as { requestToken: string };
  return request(path, {
    method,
    headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token.requestToken },
    body: payload === undefined ? undefined : JSON.stringify(payload),
  });
}

async function readSession(response: Response): Promise<Profile> {
  const session = await response.json() as Profile & { timeZone: string };
  setBusinessTimeZone(session.timeZone);
  return session;
}

export async function getProfile(): Promise<Profile> {
  return readSession(await request('account/me'));
}
export async function login(username: string, password: string): Promise<Profile> {
  return readSession(await write('account/login', 'POST', { username, password }));
}
export async function logout() {
  await write('account/logout', 'POST');
}

export function fromTimeSpan(value: string | null) {
  if (!value) return '';
  const [hoursAndDays, minutes, seconds] = value.split(':');
  const [days, hours] = hoursAndDays.includes('.') ? hoursAndDays.split('.') : ['0', hoursAndDays];
  return String(Number(days) * 24 + Number(hours)).padStart(2, '0') + ':' + minutes + ':' + seconds.slice(0, 2);
}
export function toTimeSpan(value?: string) {
  if (!value) return null;
  const [hours, minutes, seconds] = value.split(/[:.]/);
  const count = Number(hours);
  const days = Math.floor(count / 24);
  return (days ? days + '.' : '') + String(count % 24).padStart(2, '0') + ':' + minutes + ':' + seconds;
}

export type ReportSummary = {
  open: number; closed: number; skipped: number; downtimeByDirection: Record<string, number>;
};

export async function loadIncidentPage(query: string, page: number, pageSize: number, signal: AbortSignal) {
  const response = await request('reports/incidents?' + query + '&page=' + page + '&pageSize=' + pageSize, { signal });
  const batch = await response.json() as { items: ServerIncident[]; totalCount: number };
  const items: Incident[] = batch.items.map(row => ({
    id: row.id, version: row.version, createdByUserId: row.createdByUserId, ticket: row.pw, direction: row.direction, center: row.techCenter,
    category: String(row.category), network: row.networkType, element: row.serviceElement,
    stations: row.numberOfBaseStations, equipment: row.equipmentType, type: row.alarmType,
    description: row.description, start: row.beginTime, end: row.endTime || '', open: row.endTime === null,
    battery: fromTimeSpan(row.akb), generator: fromTimeSpan(row.generatorTime),
    interruption: fromTimeSpan(row.baseStationTime), notified: row.alarmWho, details: row.noticeDescription,
  }));
  return { items, totalCount: batch.totalCount };
}

export async function loadReportSummary(query: string, signal: AbortSignal): Promise<ReportSummary> {
  return (await request('reports/incidents/summary?' + query, { signal })).json();
}

export async function downloadReport(query: string) {
  const response = await request('reports/incidents.csv?' + query);
  const url = URL.createObjectURL(await response.blob());
  const link = document.createElement('a');
  link.href = url;
  link.download = 'motiv-incidents.csv';
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export async function saveIncident(values: Omit<Incident, 'id'>, id?: string) {
  await write(id ? 'incidents/' + encodeURIComponent(id) : 'incidents', id ? 'PUT' : 'POST', {
    version: id ? values.version : undefined,
    pw: values.ticket, direction: values.direction, techCenter: values.center, category: Number(values.category),
    networkType: values.network, serviceElement: values.element || '', numberOfBaseStations: values.stations,
    equipmentType: values.equipment || '', alarmType: values.type, description: values.description || '',
    beginTime: values.start, endTime: values.open ? null : values.end,
    akb: toTimeSpan(values.battery), generatorTime: toTimeSpan(values.generator),
    baseStationTime: toTimeSpan(values.interruption), alarmWho: values.notified || '', noticeDescription: values.details || '',
  });
}

export async function listUsers(): Promise<Profile[]> {
  return (await request('users')).json();
}

export async function createUser(username: string, password: string, role: Profile['role']): Promise<Profile> {
  return (await write('users', 'POST', { username, password, role })).json();
}

export async function changePassword(currentPassword: string, newPassword: string) {
  await write('account/change-password', 'POST', { currentPassword, newPassword });
}

export async function resetPassword(id: string, newPassword: string) {
  await write('users/' + encodeURIComponent(id) + '/reset-password', 'POST', { newPassword });
}

export async function closeIncident(id: string) {
  await write('incidents/' + encodeURIComponent(id) + '/close', 'POST');
}

export async function deleteIncident(id: string) {
  await write('incidents/' + encodeURIComponent(id), 'DELETE');
}
