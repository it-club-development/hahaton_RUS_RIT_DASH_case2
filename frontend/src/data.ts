export const accounts = {
  admin: 'Администратор',
  duty: 'Дежурный оператор',
  engineer: 'Инженер',
};
export type User = keyof typeof accounts;
export function isUser(value: unknown): value is User {
  return typeof value === 'string' && Object.prototype.hasOwnProperty.call(accounts, value);
}

export const fields = ['ticket','direction','center','category','network','type','element','stations','equipment','start','end','interruption','battery','generator','description','notified','details'] as const;
export type Field = typeof fields[number];
export type Incident = Partial<Record<Field, string>> & {
  id: string;
  start: string;
  open: boolean;
  createdBy?: User;
};
export const storageKey = 'motiv-incidents-v1';
export const sessionKey = 'motiv-demo-user';
export function canEdit(user: User, incident: Incident) {
  return user === 'admin' || (user === 'duty' && incident.createdBy === user);
}

export const lists = {
 direction:['ХМЯН','СВН','ЮЗН','Вся сеть'],
 center:['НБР','АСБ','ИРБ','КРТ','НТГ','НТР','НУРЛ','НВРТ','НУРГ','ОКТ','СЛХ','СОВ','СРГ','ХМН','ЕКБ','КМУ','КРГ','НСР'],
 category:['1','2','3'],network:['GSM','LTE','LTE/GSM'],
 type:['РРС','ССС','ЭЛ','RBS','МСПД','ТМ','ОПС','ПР','АВР','Аренда']
};

export function localDate(date: Date) {
 return new Date(date.getTime()-date.getTimezoneOffset()*60000).toISOString().slice(0,16);
}
function sample(): Incident[]{
 const now=Date.now();
 return [
  {id:'sample-1',ticket:'NTT-1042',direction:'СВН',center:'ЕКБ',category:'2',network:'LTE',type:'ЭЛ',element:'БС Екатеринбург-12',stations:'3',start:localDate(new Date(now-74*60000)),end:'',open:true,interruption:'00:18:00'},
  {id:'sample-2',ticket:'PW-1038',direction:'ЮЗН',center:'КМУ',category:'1',network:'GSM',type:'РРС',element:'БС Каменск-04',stations:'1',start:localDate(new Date(now-208*60000)),end:localDate(new Date(now-173*60000)),open:false,interruption:'00:12:00'},
  {id:'sample-3',ticket:'NTT-1031',direction:'ХМЯН',center:'НУРГ',category:'3',network:'LTE/GSM',type:'RBS',element:'БС Новый Уренгой-09',stations:'2',start:localDate(new Date(now-1600*60000)),end:localDate(new Date(now-1550*60000)),open:false,interruption:'00:05:00'}
 ];
}

export function duration(record: Pick<Incident, "start" | "end" | "open">) {
 if(!record.start || (!record.open && !record.end)) return null;
 const seconds=Math.floor(((record.open?Date.now():new Date(record.end || "").getTime())-new Date(record.start).getTime())/1000);
 return Number.isFinite(seconds)&&seconds>=0?seconds:null;
}
export function downtime(record: Pick<Incident, "interruption" | "stations">) {
 if(!/^\d{1,3}:[0-5]\d:[0-5]\d$/.test((record.interruption || ""))||!/^\d+$/.test(record.stations || "")) return null;
 const parts=(record.interruption || "").split(':').map(Number);
 return (parts[0]*3600+parts[1]*60+parts[2])*Number(record.stations);
}
export function clock(seconds: number | null) {
 if(seconds===null) return '—';
 return [Math.floor(seconds/3600),Math.floor(seconds/60)%60,seconds%60].map(n=>String(n).padStart(2,'0')).join(':');
}
export function dateLabel(value: string) {
 return value ? new Date(value).toLocaleString('ru-RU',{day:'2-digit',month:'2-digit',year:'numeric',hour:'2-digit',minute:'2-digit'}) : 'НВ';
}

export function filterByPeriod(rows: Incident[], hours: string, now = Date.now()) {
 if (!hours) return rows;
 const from = now - Number(hours) * 3600000;
 return rows.filter(record => {
  const start = new Date(record.start).getTime();
  return start >= from && start <= now;
 });
}

function csvCell(value: unknown) {
 let safe=String(value==null?'':value);
 if(/^[\s]*[=+@-]/.test(safe)) safe="'"+safe;
 return '"'+safe.replace(/"/g,'""')+'"';
}
export function downloadCsv(records: Incident[]) {
 const headings=['PW / NTT','Направление','ТЦ','Категория','Тип сети','Сетевой элемент','Количество БС','Тип оборудования','Тип аварии','Краткое описание','Начало','Окончание','АКБ','ГУ','Длительность','Прерывание связи','Общий простой','Кого уведомили','Подробное описание'];
 const rows=records.map(row=>[row.ticket,row.direction,row.center,row.category,row.network,row.element,row.stations,row.equipment,row.type,row.description,row.start,row.end||'НВ',row.battery,row.generator,clock(duration(row)),row.interruption,clock(downtime(row)),row.notified,row.details]);
 const csv=[headings, ...rows].map(row=>row.map(csvCell).join(';')).join('\r\n');
 const url=URL.createObjectURL(new Blob(['\uFEFF',csv],{type:'text/csv;charset=utf-8'}));
 const link=document.createElement('a');
 link.href=url;link.download='motiv-incidents.csv';
 document.body.appendChild(link);link.click();link.remove();
 setTimeout(()=>URL.revokeObjectURL(url),1000);
}

export function loadIncidents(): Incident[] {
  try {
    const saved: unknown = JSON.parse(localStorage.getItem(storageKey) || "null");
    if (Array.isArray(saved) && saved.every(row => row && typeof row.id === "string" && typeof row.start === "string" && typeof row.open === "boolean" && fields.every(field => row[field] === undefined || typeof row[field] === "string"))) return saved;
  } catch { /* Fall back to demonstration data. */ }
  return sample();
}
