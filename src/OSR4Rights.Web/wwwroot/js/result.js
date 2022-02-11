const showLogsButton = document.getElementById('show-logs-button');
const logsList = document.getElementById('logs-list');

function toggle() {
    logsList.classList.toggle('hidden-display-none');
}

showLogsButton.addEventListener('click', toggle)
