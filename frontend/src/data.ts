export const fields = ['ticket','direction','center','category','network','type','element','stations','equipment','start','end','interruption','battery','generator','description','notified','details'] as const;
export type Field = typeof fields[number];
export type Incident = Partial<Record<Field, string>> & {
  id: string;
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

export function localDate(date: Date) {
 return new Date(date.getTime()-date.getTimezoneOffset()*60000).toISOString().slice(0,16);
}
export function duration(record: Pick<Incident, "start" | "end" | "open">) {
 if(!record.start || (!record.open && !record.end)) return null;
 const seconds=Math.floor(((record.open?Date.now():new Date(record.end || "").getTime())-new Date(record.start).getTime())/1000);
 return Number.isFinite(seconds) ? Math.max(0, seconds) : null;
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



