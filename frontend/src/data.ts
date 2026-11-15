export const fields = ['ticket','direction','center','category','network','type','element','stations','equipment','start','end','interruption','battery','generator','description','notified','details'] as const;
export type Field = typeof fields[number];
export type Incident = Partial<Record<Field, string>> & {
  id: string;
  version?: string;
  createdByUserId?: string | null;
  start: string;
  open: boolean;
};

export const lists = {
 direction:['ХМЯН','СВН','ЮЗН','Вся сеть'],
 center:['НБР','АСБ','ИРБ','КРТ','НТГ','НТР','НУРЛ','НВРТ','НУРГ','ОКТ','СЛХ','СОВ','СРГ','ХМН','ЕКБ','КМУ','КРГ','НСР'],
 category:['1','2','3'],network:['GSM','LTE','LTE/GSM'],
 type:['РРС','ССС','ЭЛ','RBS','МСПД','ТМ','ОПС','ПР','АВР','Аренда']
};

let businessClock = makeBusinessClock('UTC');

function makeBusinessClock(timeZone: string) {
 return new Intl.DateTimeFormat('en-GB', {
  timeZone, year: 'numeric', month: '2-digit', day: '2-digit',
  hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false,
 });
}

export function setBusinessTimeZone(timeZone: string) {
 businessClock = makeBusinessClock(timeZone);
}

export function localDate(date: Date) {
 const parts = new Map(businessClock.formatToParts(date).map(part => [part.type, part.value]));
 return parts.get('year') + '-' + parts.get('month') + '-' + parts.get('day')
  + 'T' + (parts.get('hour') === '24' ? '00' : parts.get('hour')) + ':' + parts.get('minute') + ':' + parts.get('second');
}

export function duration(record: Pick<Incident, "start" | "end" | "open">) {
 if (!record.start || (!record.open && !record.end)) return null;
 // Dates saved in MariaDB represent local server time without a UTC offset.
 const end = record.open ? localDate(new Date()) : record.end!;
 const seconds = Math.floor((Date.parse(end + 'Z') - Date.parse(record.start + 'Z')) / 1000);
 return Number.isFinite(seconds) ? Math.max(0, seconds) : null;
}
export function downtime(record: Pick<Incident, "interruption" | "stations">) {
 if(!/^\d{1,3}([:.])[0-5]\d\1[0-5]\d$/.test((record.interruption || ""))||!/^\d+$/.test(record.stations || "")) return null;
 const parts=(record.interruption || "").split(/[:.]/).map(Number);
 return (parts[0]*3600+parts[1]*60+parts[2])*Number(record.stations);
}
export function clock(seconds: number | null) {
 if(seconds===null) return '—';
 return [Math.floor(seconds/3600),Math.floor(seconds/60)%60,seconds%60].map(n=>String(n).padStart(2,'0')).join(':');
}
export function dateLabel(value: string) {
 return value ? new Date(value + "Z").toLocaleString('ru-RU',{timeZone:'UTC',day:'2-digit',month:'2-digit',year:'numeric',hour:'2-digit',minute:'2-digit'}) : 'НВ';
}



