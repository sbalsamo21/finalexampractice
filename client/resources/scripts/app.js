// API Configuration
function getApiUrl() {
    return document.getElementById('apiUrl').value.trim() || 'http://localhost:5102/api/tasks';
}

// Message handling
function showMessage(message, type = 'error') {
    const container = document.getElementById('messageContainer');
    container.innerHTML = `<div class="${type}">${message}</div>`;
    setTimeout(() => {
        container.innerHTML = '';
    }, 5000);
}

// Load all tasks
async function loadTasks() {
    const loadingMessage = document.getElementById('loadingMessage');
    const tableContainer = document.getElementById('tasksTableContainer');
    const emptyState = document.getElementById('emptyState');
    const tableBody = document.getElementById('tasksTableBody');

    loadingMessage.style.display = 'block';
    tableContainer.style.display = 'none';
    emptyState.style.display = 'none';

    try {
        const apiUrl = getApiUrl();
        const response = await fetch(apiUrl);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const tasks = await response.json();

        loadingMessage.style.display = 'none';

        if (tasks.length === 0) {
            emptyState.style.display = 'block';
            return;
        }

        tableBody.innerHTML = '';
        tasks.forEach(task => {
            const row = createTaskRow(task);
            tableBody.appendChild(row);
        });

        tableContainer.style.display = 'block';
    } catch (error) {
        loadingMessage.style.display = 'none';
        showMessage(`Failed to load tasks: ${error.message}. Make sure your API is running at ${getApiUrl()}`, 'error');
        console.error('Error loading tasks:', error);
    }
}

// Create table row for a task
function createTaskRow(task) {
    const row = document.createElement('tr');
    
    const statusClass = `status-${task.status.toLowerCase().replace(' ', '-')}`;
    
    row.innerHTML = `
        <td>${task.taskId}</td>
        <td><strong>${escapeHtml(task.name)}</strong></td>
        <td>${escapeHtml(task.description || '')}</td>
        <td><span class="status-badge ${statusClass}">${escapeHtml(task.status)}</span></td>
        <td>${formatDate(task.createdAt)}</td>
        <td>
            <div class="action-buttons">
                <button class="btn btn-warning btn-small" onclick="editTask(${task.taskId})">Edit</button>
                <button class="btn btn-danger btn-small" onclick="deleteTask(${task.taskId})">Delete</button>
            </div>
        </td>
    `;
    
    return row;
}

// Format date
function formatDate(dateString) {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

// Escape HTML to prevent XSS
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Add/Edit task form submission
document.getElementById('taskForm').addEventListener('submit', async (e) => {
    e.preventDefault();

    const taskId = document.getElementById('taskId').value;
    const taskName = document.getElementById('taskName').value.trim();
    const taskDescription = document.getElementById('taskDescription').value.trim();
    const taskStatus = document.getElementById('taskStatus').value;

    if (!taskName) {
        showMessage('Task name is required', 'error');
        return;
    }

    const taskData = {
        name: taskName,
        description: taskDescription || null,
        status: taskStatus
    };

    try {
        const apiUrl = getApiUrl();
        let response;

        if (taskId) {
            // Update existing task
            response = await fetch(`${apiUrl}/${taskId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(taskData)
            });
        } else {
            // Create new task
            response = await fetch(apiUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(taskData)
            });
        }

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({ message: 'Unknown error' }));
            throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        showMessage(taskId ? 'Task updated successfully!' : 'Task created successfully!', 'success');
        resetForm();
        await loadTasks();
    } catch (error) {
        showMessage(`Failed to save task: ${error.message}`, 'error');
        console.error('Error saving task:', error);
    }
});

// Edit task
async function editTask(taskId) {
    try {
        const apiUrl = getApiUrl();
        const response = await fetch(`${apiUrl}/${taskId}`);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const task = await response.json();

        document.getElementById('taskId').value = task.taskId;
        document.getElementById('taskName').value = task.name;
        document.getElementById('taskDescription').value = task.description || '';
        document.getElementById('taskStatus').value = task.status;

        document.getElementById('formTitle').textContent = 'Edit Task';
        document.getElementById('submitBtn').textContent = 'Update Task';
        document.getElementById('cancelBtn').style.display = 'inline-block';

        // Scroll to form
        document.querySelector('.form-section').scrollIntoView({ behavior: 'smooth' });
    } catch (error) {
        showMessage(`Failed to load task for editing: ${error.message}`, 'error');
        console.error('Error loading task:', error);
    }
}

// Delete task
async function deleteTask(taskId) {
    if (!confirm('Are you sure you want to delete this task?')) {
        return;
    }

    try {
        const apiUrl = getApiUrl();
        const response = await fetch(`${apiUrl}/${taskId}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        showMessage('Task deleted successfully!', 'success');
        await loadTasks();
    } catch (error) {
        showMessage(`Failed to delete task: ${error.message}`, 'error');
        console.error('Error deleting task:', error);
    }
}

// Reset form
function resetForm() {
    document.getElementById('taskForm').reset();
    document.getElementById('taskId').value = '';
    document.getElementById('formTitle').textContent = 'Add New Task';
    document.getElementById('submitBtn').textContent = 'Add Task';
    document.getElementById('cancelBtn').style.display = 'none';
}

// Load tasks on page load
document.addEventListener('DOMContentLoaded', () => {
    loadTasks();
});

