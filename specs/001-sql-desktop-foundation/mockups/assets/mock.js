/* Review-only fixtures and UI simulation. No application API, database, or financial persistence. */
'use strict';
const MOCK = {
  date: '2026-11-23', revision: 'US3 · 2026-11-23 · footing + row tracking',
  accounts: [
    {path:'Assets:Checking',group:'Banks',icon:'account_balance'},
    {path:'Assets:Savings',group:'Banks',icon:'savings'},
    {path:'Liabilities:Everyday Card',group:'Credit Cards',icon:'credit_card'},
    {path:'Assets:Brokerage',group:'Investments',icon:'pie_chart'},
    {path:'Liabilities:Home Loan',group:'Loans Payable',icon:'description'},
    {path:'Expenses:Groceries',group:'Other',type:'Expenses',icon:'shopping_basket'},
    {path:'Expenses:Dining',group:'Other',type:'Expenses',icon:'restaurant'},
    {path:'Expenses:Utilities',group:'Other',type:'Expenses',icon:'lightbulb'},
    {path:'Expenses:Household and Family:Travel and Accommodation Domestic',group:'Other',type:'Expenses',icon:'flight'},
    {path:'Expenses:Household and Family:Travel and Accommodation International',group:'Other',type:'Expenses',icon:'flight'},
    {path:'Income:Salary',group:'Other',type:'Income',icon:'payments'},
    {path:'Equity:Opening Balance',group:'Other',type:'Equity',icon:'balance'}
  ],
  entries: []
};
const fixtureRows = [
  ['2026-11-01','Opening balance','Initial checking balance','Equity:Opening Balance',100000,true],
  ['2026-11-02','Northwind Payroll','November salary','Income:Salary',280000,true],
  ['2026-11-03','Maple Market','Weekly groceries','Expenses:Groceries',-4590,true],
  ['2026-11-04','Oak Café','Coffee and lunch','Expenses:Dining',-1850,false],
  ['2026-11-05','City Utilities','Electricity','Expenses:Utilities',-9250,true],
  ['2026-11-06','Savings transfer','Monthly reserve','Assets:Savings',-30000,true],
  ['2026-11-07','Maple Market','Weekly groceries','Expenses:Groceries',-6325,true],
  ['2026-11-09','Corner Shop','Household split','Expenses:Groceries',-2500,true],
  ['2026-11-10','Oak Café','Lunch','Expenses:Dining',-1450,false],
  ['2026-11-11','City Utilities','Water','Expenses:Utilities',-4200,false],
  ['2026-11-12','Maple Market','Weekly groceries','Expenses:Groceries',-5780,true],
  ['2026-11-14','Everyday Card','Card payment','Liabilities:Everyday Card',-12000,false],
  ['2026-11-15','Northwind Payroll','Expense reimbursement','Income:Salary',18500,true],
  ['2026-11-16','Oak Café','Family lunch','Expenses:Dining',-3800,false],
  ['2026-11-18','Maple Market','Weekly groceries','Expenses:Groceries',-7190,false],
  ['2026-11-20','City Utilities','Internet','Expenses:Utilities',-6500,false],
  ['2026-11-21','Savings transfer','Additional reserve','Assets:Savings',-10000,false],
  ['2026-11-22','Oak Café','Weekend breakfast','Expenses:Dining',-2450,false]
];
MOCK.entries = fixtureRows.map((r,i)=>({id:i+1,date:r[0],bankDate:'',payee:r[1],description:r[2],lines:[
  {account:'Assets:Checking',debit:r[4]>0?r[4]:0,credit:r[4]<0?-r[4]:0,status:r[5]?2:0},
  {account:r[3],debit:r[4]<0?-r[4]:0,credit:r[4]>0?r[4]:0,status:0}
]}));
MOCK.entries[7].lines=[{account:'Expenses:Groceries',debit:2000,credit:0,status:2},{account:'Expenses:Dining',debit:500,credit:0,status:1},{account:'Assets:Checking',debit:0,credit:2500,status:2}];
const params = new URLSearchParams(location.search);
const $ = id => document.getElementById(id);
const money = cents => (cents/100).toLocaleString('en-US',{minimumFractionDigits:2,maximumFractionDigits:2});
const escapeHtml = value => String(value??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const statusName = n => ['Uncleared','Reconciling','Cleared'][n] || 'Uncleared';
const symbol = n => ['', 'R', '✓'][n] || '';
const summary = entry => `${escapeHtml(entry.payee)}${entry.payee && entry.description ? ' — ' : ''}${entry.description?`<em>${escapeHtml(entry.description)}</em>`:''}`;
const stateLinks = {
  main:[['main.html','Populated register'],['main.html?state=empty','Empty register'],['main.html?state=loading','Loading'],['main.html?state=error','Read failure'],['main.html?state=reset','Reset confirmation']],
  journal:[['journal.html','New entry'],['journal.html?preset=split','Balanced split'],['journal.html?preset=unbalanced','Out of balance'],['journal.html?preset=invalid','Validation errors'],['journal.html?preset=edit','Edit cleared lines'],['journal.html?preset=warning','Reset warning'],['journal.html?preset=conflict','Save conflict'],['journal.html?preset=error','Unconfirmed save'],['journal.html?preset=busy','Pending save'],['journal.html?preset=long','Long split']]
};
function installReviewChrome(screen){
  const bar=document.createElement('div');bar.className='mock-bar';
  bar.innerHTML=`<strong>HTML mock-up · ${MOCK.revision}</strong><span>Synthetic data, simulated operations</span><a href="index.html">Review index</a>`;
  const controls=document.createElement('details');controls.className='mock-controls';
  const links=screen==='index'?[...stateLinks.main,...stateLinks.journal]:stateLinks[screen];
  controls.innerHTML=`<summary>Mock controls — not part of design</summary><div class="controls"><label>Review state <select id="review-state"><option value="">Choose a state…</option>${links.map(([url,label])=>`<option value="${url}">${label}</option>`).join('')}</select></label>${screen==='journal'?'<label>Next save outcome <select id="outcome"><option value="success">Success</option><option value="conflict">Conflict</option><option value="error">Unconfirmed failure</option></select></label>':''}<span>Persona: book owner · USD · UTC · fixed review date ${MOCK.date}</span></div><p>New and Edit open the journal page. Save and Cancel return to the register. Continue retains the date. Operations use fixtures only; changing pages resets the synthetic data.</p>`;
  document.body.prepend(controls);document.body.prepend(bar);
  $('review-state').onchange=e=>{if(e.target.value)location.href=e.target.value;};
}
function frameTitle(title){return `<div class="titlebar"><span class="material-symbols-outlined app-glyph" aria-hidden="true">account_balance_wallet</span>${title}<span class="window-actions" aria-hidden="true">—　□　×</span></div>`;}

function renderMain(){
  installReviewChrome('main');
  let current=MOCK.accounts.find(a=>a.path===params.get('account')) || MOCK.accounts[0];
  let selected=+(params.get('entry') || 8), state=params.get('state') || 'default';
  $('app-title').innerHTML=frameTitle('Personal demo — Fat Stacks.NET');
  function rowsForAccount(){let balance=0;return MOCK.entries.flatMap(e=>{
    const lines=e.lines.filter(l=>l.account===current.path);if(!lines.length)return [];
    const delta=lines.reduce((s,l)=>s+l.debit-l.credit,0);balance+=delta;
    return [{...e,delta,balance,counter:e.lines.filter(l=>l.account!==current.path).map(l=>l.account).join('; '),status:lines.every(l=>l.status===lines[0].status)?lines[0].status:3}];
  });}
  function draw(){
    const groups=[['Banks','account_balance'],['Credit Cards','credit_card'],['Investments','pie_chart'],['Loans Payable','description'],['Other','folder']];
    $('account-tabs').innerHTML=groups.map(([g,icon])=>`<a href="main.html?account=${encodeURIComponent(MOCK.accounts.find(a=>a.group===g).path)}" title="${g}" aria-label="${g}" class="${current.group===g?'active':''}"><span class="material-symbols-outlined" aria-hidden="true">${icon}</span></a>`).join('');
    $('group-title').textContent=current.group.toUpperCase();
    const accounts=MOCK.accounts.filter(a=>a.group===current.group);
    $('accounts').innerHTML=accounts.map((a,i)=>`${a.type && (i===0||accounts[i-1].type!==a.type)?`<div class="rail-group">${a.type}</div>`:''}<a class="account-link ${current.path===a.path?'active':''}" href="main.html?account=${encodeURIComponent(a.path)}" title="${escapeHtml(a.path)}">${escapeHtml(a.path.split(':').slice(1).join(':'))}<small>${money(state==='empty'?0:MOCK.entries.flatMap(e=>e.lines).filter(l=>l.account===a.path).reduce((s,l)=>s+l.debit-l.credit,0))} USD</small></a>`).join('');
    $('account-title').textContent=current.path.split(':').at(-1);$('account-path').textContent=current.path;
    const all=rowsForAccount(), query=$('search').value.toLowerCase();
    const rows=all.filter(e=>e.description.toLowerCase().includes(query)||e.counter.toLowerCase().includes(query));
    $('current-balance').textContent=`USD ${money(state==='empty'?0:(all.at(-1)?.balance || 0))}`;
    $('transaction-count').textContent=`${state==='empty'?0:rows.length} transactions`;
    $('register-body').innerHTML=state==='default'||state==='reset'||state==='saved'?rows.map(e=>`<tr tabindex="0" data-entry="${e.id}" class="${e.id===selected?'selected':''}" aria-selected="${e.id===selected}"><td>${e.date}</td><td><div class="summary-text" title="${escapeHtml(e.payee+' — '+e.description)}">${summary(e)}</div></td><td title="${escapeHtml(e.counter)}">${escapeHtml(e.counter)}</td><td class="center" aria-label="${e.status===3?'Mixed':statusName(e.status)}" title="${e.status===3?'Mixed':statusName(e.status)}">${e.status===3?'M':symbol(e.status)}</td><td class="num">${e.delta<0?money(-e.delta):''}</td><td class="num">${e.delta>0?money(e.delta):''}</td><td class="num">${money(e.balance)}</td></tr>`).join(''):'';
    $('register-table').hidden=!['default','reset','saved'].includes(state);
    $('empty-state').hidden=['default','reset','saved'].includes(state)&&rows.length>0;
    const message=state==='loading'?['Loading transactions…','The book remains responsive.']:state==='error'?['Unable to load transactions','Check the connection and use Reload to try again.']:['No transactions to show',query?'No descriptions or counter-accounts match your search.':'Create a new entry to get started.'];
    $('empty-state').innerHTML=`<h2>${message[0]}</h2><p class="muted">${message[1]}</p>`;
    const entry=rows.find(e=>e.id===selected);
    $('details').hidden=!entry||!['default','reset','saved'].includes(state);
    if(entry){$('detail-summary').innerHTML=summary(entry)+' <span class="muted">USD</span>';$('entry-id').textContent=`Journal entry ${entry.id}`;
      $('detail-body').innerHTML=entry.lines.map(l=>`<tr><td><a href="main.html?account=${encodeURIComponent(l.account)}&entry=${entry.id}">${escapeHtml(l.account)}</a></td><td class="num">${money(l.debit)}</td><td class="num">${money(l.credit)}</td><td>${statusName(l.status)}</td><td></td><td></td></tr>`).join('');}
    $('edit-entry').href=`journal.html?preset=edit&entry=${entry?.id||8}&account=${encodeURIComponent(current.path)}`;$('new-entry').href=`journal.html?account=${encodeURIComponent(current.path)}`;
    $('edit-entry').setAttribute('aria-disabled',String(!entry));
    $('register-status').textContent=state==='error'?'Register could not be loaded. Check the connection and refresh.':state==='loading'?'Loading register…':`${state==='empty'?0:rows.length} transactions shown`;
    document.querySelectorAll('[data-entry]').forEach(tr=>{tr.onclick=()=>{selected=+tr.dataset.entry;draw();};tr.ondblclick=()=>location.href=`journal.html?preset=edit&entry=${tr.dataset.entry}`;tr.onkeydown=e=>{if(e.key==='Enter'){selected=+tr.dataset.entry;draw();}};});
  }
  $('edit-entry').onclick=e=>{if($('edit-entry').getAttribute('aria-disabled')==='true')e.preventDefault();};
  $('search').oninput=draw;$('reload').onclick=()=>{state='default';draw();};
  if(state==='reset'){
    const e=MOCK.entries[7];e.lines[0].status=0;e.lines[2].status=0;e.lines[0].debit=2100;e.lines[2].credit=2600;
    $('save-notice').hidden=false;$('save-notice-text').textContent='Entry saved. Reset to Uncleared: Line 1 (Expenses:Groceries); Line 3 (Assets:Checking).';
  }
  $('dismiss-notice').onclick=()=>{$('save-notice').hidden=true;};
  document.onkeydown=e=>{if(e.target.matches('input,select,textarea'))return;if(e.key.toLowerCase()==='n')location.href=$('new-entry').href;if(e.key.toLowerCase()==='e')location.href=$('edit-entry').href;if(e.key==='F5'){e.preventDefault();state='default';draw();}if(e.ctrlKey&&e.key.toLowerCase()==='f'){e.preventDefault();$('search').focus();}};
  draw();
}

function renderJournal(){
  installReviewChrome('journal');$('app-title').innerHTML=frameTitle('Journal entry');
  const preset=params.get('preset') || 'new', fixture=MOCK.entries.find(e=>e.id===+(params.get('entry')||8));
  let draft=[], busy=false, errors=[], selectedOutcome='success';
  function fromFixture(){return fixture.lines.map(l=>({account:l.account,debit:l.debit?money(l.debit).replaceAll(',',''):'',credit:l.credit?money(l.credit).replaceAll(',',''):'',status:l.status,original:{...l}}));}
  const blank=()=>({account:'',debit:'',credit:'',status:0,original:null});
  if(preset==='new'){draft=[blank(),blank()];draft[0].account=params.get('account') || 'Assets:Checking';}
  else if(['edit','warning','conflict','error'].includes(preset)){draft=fromFixture();$('payee').value=fixture.payee;$('description').value=fixture.description;}
  else{draft=[{...blank(),account:'Expenses:Groceries',debit:'20.00'},{...blank(),account:'Expenses:Dining',debit:'5.00'},{...blank(),account:'Assets:Checking',credit:'25.00'}];$('payee').value='Corner Shop';$('description').value='Household split';}
  $('date').value=['edit','warning','conflict','error'].includes(preset)?fixture.date:MOCK.date;
  if(preset==='warning'){draft[0].debit='21.00';draft[2].credit='26.00';}
  if(preset==='unbalanced')draft[2].credit='24.00';
  if(preset==='invalid'){draft[0].debit='abc';errors=['Line 1 · Debit: Enter a decimal amount with at most two decimal places; trailing zeros are allowed.','Line 1 · Amount: Enter one positive Debit or Credit.','Balance: Total Debit must equal total Credit.'];}
  if(preset==='long'){draft=Array.from({length:29},()=>({...blank(),account:MOCK.accounts[8].path,debit:'1.00'}));draft.push({...blank(),account:'Assets:Checking',credit:'29.00'});}
  if(preset==='conflict'){$('journal-status').textContent='This entry changed. Reload before retrying. Your draft is retained.';selectedOutcome='conflict';}
  if(preset==='error'){$('journal-status').textContent='Save was not confirmed. Keep this draft and reload before retrying.';selectedOutcome='error';}
  $('outcome').value=selectedOutcome;$('outcome').onchange=e=>selectedOutcome=e.target.value;
  $('account-options').innerHTML=MOCK.accounts.map(a=>`<option value="${escapeHtml(a.path)}"></option>`).join('');
  const parse=text=>{if(!text.trim())return 0;const clean=text.replaceAll(',','');if(!/^\d+(?:\.\d*)?$/.test(clean))return null;const [whole,fraction='']=clean.split('.');if(/[1-9]/.test(fraction.slice(2)))return null;const n=Number(whole)*100+Number((fraction+'00').slice(0,2));return Number.isSafeInteger(n)&&n<=9999999999?n:null;};
  function totals(){let debit=0,credit=0;for(const l of draft){const d=parse(l.debit),c=parse(l.credit);if(d===null||c===null)return null;debit+=d;credit+=c;}return{debit,credit,unbalanced:debit!==credit};}
  function resetLines(){return draft.flatMap((l,i)=>l.original&&l.original.status>0&&(l.account!==l.original.account||(parse(l.debit)!==null&&parse(l.credit)!==null&&(parse(l.debit)!==l.original.debit||parse(l.credit)!==l.original.credit)))?i:[]);}
  function resetLabels(){return resetLines().map(i=>`Line ${i+1} (${draft[i].account||draft[i].original.account})`);}
  function update(){const t=totals();let vh,status,debitCell='',creditCell='';
    if(t){const diff=money(Math.abs(t.debit-t.credit));const word=t.debit>t.credit?'Credits':'Debits';
      if(t.unbalanced){vh=`Debit: ${money(t.debit)}. Credit: ${money(t.credit)}. ${word} short by ${diff}.`;status=`<span class="error">${word} short by ${diff}</span>`;}
      else{vh=`Debit: ${money(t.debit)}. Credit: ${money(t.credit)}. Balanced.`;status='<span class="muted">Balanced</span>';}
      status=`<span class="foot-label">Totals:</span>${status}`;
      debitCell=`<span${t.unbalanced&&t.debit>t.credit?' class="error"':''}>${money(t.debit)}</span>`;
      creditCell=`<span${t.unbalanced&&t.credit>t.debit?' class="error"':''}>${money(t.credit)}</span>`;}
    else{vh='Enter valid amounts to check the balance.';status='<span class="muted">Enter valid amounts to check the balance.</span>';}
    $('balance-summary').innerHTML=`<span class="vh">${vh}</span><div class="footing-cells" aria-hidden="true"><span></span><span class="status-foot">${status}</span><span></span><span class="amount-foot">${debitCell}</span><span class="amount-foot">${creditCell}</span><span></span></div>`;$('save').disabled=busy||!!t?.unbalanced;$('continue').disabled=busy||!!t?.unbalanced;
    const reset=resetLabels();const warned=new Set(resetLines());document.querySelectorAll('.posting-row').forEach((row,i)=>row.querySelector('.line-no')?.classList.toggle('warn',warned.has(i)));$('reset-warning').hidden=!reset.length;$('reset-warning').textContent=reset.length?'Saving will reset to Uncleared: '+reset.join('; ')+'.':'';
    errors=errors.filter(e=>{
      if(e.startsWith('AccountingDate:')||e.startsWith('Date:'))return !/^\d{4}-\d{2}-\d{2}$/.test($('date').value);
      if(e.startsWith('Balance:'))return !t||t.unbalanced;
      const m=/^Line (\d+) · ([^:]+): /.exec(e);if(!m)return true;
      const l=draft[+m[1]-1];if(!l)return false;
      const field=m[2].toLowerCase(),d=parse(l.debit),c=parse(l.credit);
      if(field==='account')return !MOCK.accounts.some(a=>a.path===l.account);
      if(field==='debit')return d===null;
      if(field==='credit')return c===null;
      if(field==='amount')return d===null||c===null||(!d&&!c);
      return true;
    });
    if(!errors.length&&$('journal-status').textContent==='Correct the fields below.')$('journal-status').textContent='';
    const amountMsg='Enter a decimal amount with at most two decimal places; trailing zeros are allowed.';
    const lineErrMap=new Map();
    for(const e of errors){const m=/^Line (\d+) · ([^:]+): (.+)$/.exec(e);if(m){const idx=+m[1]-1;if(!lineErrMap.has(idx))lineErrMap.set(idx,{});lineErrMap.get(idx)[m[2].toLowerCase()]=m[3];}}
    document.querySelectorAll('.posting-row').forEach((row,i)=>{
      const l=draft[i],fe=lineErrMap.get(i)||{},dBad=Boolean(l.debit.trim()&&parse(l.debit)===null),cBad=Boolean(l.credit.trim()&&parse(l.credit)===null);
      const accErr=fe.account||'',debErr=fe.debit||(dBad?amountMsg:'')||(!cBad&&fe.amount?fe.amount:''),credErr=fe.credit||(cBad?amountMsg:'')||(!dBad&&fe.amount?fe.amount:'');
      const setInvalid=(field,msg,fallbackTitle)=>{const el=row.querySelector(`[data-field="${field}"]`);if(!el)return;el.classList.toggle('invalid',Boolean(msg));if(msg){el.setAttribute('aria-invalid','true');el.title=msg;}else{el.removeAttribute('aria-invalid');if(fallbackTitle)el.title=fallbackTitle;else el.removeAttribute('title');}};
      setInvalid('account',accErr,l.account);setInvalid('debit',debErr,'');setInvalid('credit',credErr,'');
      row.querySelector('.line-no')?.classList.toggle('err',Boolean(accErr||debErr||credErr));
    });
    $('errors').innerHTML=errors.map(e=>`<div>${escapeHtml(e)}</div>`).join('');document.querySelectorAll('.remove-line').forEach(b=>b.disabled=busy||draft.length<=2);}
  function draw(){
    $('postings').innerHTML=draft.map((l,i)=>`<div class="posting-row" data-line="${i}"><span class="line-no" aria-hidden="true">${i+1}</span><input data-field="account" list="account-options" autocomplete="off" aria-label="Account, line ${i+1}" title="${escapeHtml(l.account)}" value="${escapeHtml(l.account)}"><span class="state-symbol" title="${statusName(l.status)}" aria-label="${statusName(l.status)}">${symbol(l.status)}</span><input class="amount" data-field="debit" inputmode="decimal" aria-label="Debit, line ${i+1}" value="${escapeHtml(l.debit)}"><input class="amount" data-field="credit" inputmode="decimal" aria-label="Credit, line ${i+1}" value="${escapeHtml(l.credit)}"><button class="remove-line" tabindex="-1" aria-label="Remove posting ${i+1}" title="Remove posting (Ctrl+Shift+Delete; at least two required)">×</button></div>`).join('');
    document.querySelectorAll('.posting-row').forEach(row=>{
      const i=+row.dataset.line;
      row.querySelectorAll('input').forEach(input=>{
        const field=input.dataset.field;
        input.onfocus=()=>{if(field==='account')input.select();};
        input.oninput=()=>{draft[i][field]=input.value;if(['debit','credit'].includes(field)&&input.value.trim()){const opposite=field==='debit'?'credit':'debit';draft[i][opposite]='';row.querySelector(`[data-field="${opposite}"]`).value='';}update();};
        input.onblur=()=>{if(field!=='account'&&input.value.trim()){const n=parse(input.value);if(n!==null){draft[i][field]=money(n).replaceAll(',','');input.value=draft[i][field];}}};
        input.onkeydown=e=>{if(e.ctrlKey&&e.shiftKey&&e.key==='Delete'){e.preventDefault();remove(i);}if(field==='credit'&&i===draft.length-1&&e.key==='Tab'&&!e.shiftKey&&totals()?.unbalanced){e.preventDefault();add();}};
      });row.querySelector('button').onclick=()=>remove(i);
    });update();
  }
  function focusAccount(i){document.querySelector(`[data-line="${i}"] input`)?.focus();}
  function add(){if(busy)return;draft.push(blank());draw();focusAccount(draft.length-1);}
  function remove(i){if(busy||draft.length<=2)return;draft.splice(i,1);draw();focusAccount(Math.min(i,draft.length-1));}
  function save(keepOpen){
    if(busy||totals()?.unbalanced)return;
    errors=[];
    if(!/^\d{4}-\d{2}-\d{2}$/.test($('date').value))errors.push('AccountingDate: Choose a valid accounting date.');
    draft.forEach((l,i)=>{if(!MOCK.accounts.some(a=>a.path===l.account))errors.push(`Line ${i+1} · Account: Choose an available account.`);if(parse(l.debit)===null||parse(l.credit)===null)errors.push(`Line ${i+1} · Amount: Enter an exact amount with at most two decimal places; trailing zeros are allowed.`);else if(!parse(l.debit)&&!parse(l.credit))errors.push(`Line ${i+1} · Amount: Enter one positive Debit or Credit.`);});
    if(errors.length){$('journal-status').textContent='Correct the fields below.';update();return;}
    if(selectedOutcome!=='success'){$('journal-status').textContent=selectedOutcome==='conflict'?'This entry changed. Reload before retrying. Your draft is retained.':'Save was not confirmed. Keep this draft and reload before retrying.';return;}
    const reset=resetLabels();
    if(keepOpen){$('journal-status').textContent=reset.length?'Entry saved. Reset to Uncleared: '+reset.join('; ')+'.':'Entry saved.';draft=[blank(),blank()];$('payee').value='';$('description').value='';$('bank-date').value='';draw();$('date').focus();$('date').select();}
    else location.href=reset.length?'main.html?state=reset&entry=8':'main.html?state=saved';
  }
  $('add-line').onclick=add;$('save').onclick=()=>save(false);$('continue').onclick=()=>save(true);$('cancel').onclick=()=>{if(!busy)location.href='main.html';};
  [$('date'),$('bank-date')].forEach(input=>input.onkeydown=e=>{if(!['-','+','t','T'].includes(e.key)||e.ctrlKey||e.altKey)return;e.preventDefault();let value=input.value||MOCK.date;if(e.key.toLowerCase()==='t')value=MOCK.date;else{const d=new Date(value+'T12:00:00Z');if(!Number.isFinite(d.getTime())){errors=['Date: Enter a valid date or press T for today.'];update();return;}d.setUTCDate(d.getUTCDate()+(e.key==='+'?1:-1));value=d.toISOString().slice(0,10);}input.value=value;update();});
  $('date').oninput=update;
  document.onkeydown=e=>{if(e.ctrlKey&&e.key==='Enter'){e.preventDefault();save(false);}if(e.key==='Escape'&&!busy)location.href='main.html';};
  draw();
  if(preset==='busy'){busy=true;$('journal-status').textContent='Saving entry…';$('journal-fields').classList.add('busy');$('postings').classList.add('busy');$('cancel').disabled=true;$('add-line').disabled=true;update();}
  else {$('date').focus();$('date').select();}
}
document.addEventListener('DOMContentLoaded',()=>{const screen=document.body.dataset.screen;if(screen==='main')renderMain();else if(screen==='journal')renderJournal();else installReviewChrome('index');});
