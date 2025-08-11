const api = '/api/studygroups';

async function createGroup(e) {
    e.preventDefault();
    await fetch(api, {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({
            name: document.getElementById('groupName').value,
            subject: document.getElementById('subject').value
        })
    });
    e.target.reset();
    loadGroups();
}

function rowCells(g) {
    const members = (g.members || []).map(m => m.userName).filter(Boolean).join(', ');
    return `<td>${g.studyGroupId}</td><td>${g.name}</td><td>${g.subject}</td><td>${members}</td>`;
}

async function loadGroups() {
    const subject = document.getElementById('filterSubject').value;
    const asc = document.getElementById('sortOrder').value === 'asc';
    const url = subject ? `${api}?subject=${encodeURIComponent(subject)}` : api;
    const list = await (await fetch(url)).json();
    list.sort((a, b) => (new Date(a.createDate) - new Date(b.createDate)) * (asc ? 1 : -1));
    document.getElementById('groups').innerHTML = list.map(g => `<tr data-id="${g.studyGroupId}">${rowCells(g)}</tr>`).join('');
}

async function updateMembership(e) {
    e.preventDefault();
    const id = document.getElementById('memberGroupId').value;
    const user = document.getElementById('memberUserId').value;
    const action = e.submitter?.value || 'join';
    const res = await fetch(`${api}/${id}/${action}?userId=${user}`, {method: 'POST'});
    if (res.ok) await refreshGroupRow(id);
}

async function refreshGroupRow(id) {
    const row = document.querySelector(`#groups tr[data-id="${id}"]`);
    if (!row) return;
    const g = await (await fetch(`${api}/${id}`)).json();
    row.innerHTML = rowCells(g);
}

loadGroups();
