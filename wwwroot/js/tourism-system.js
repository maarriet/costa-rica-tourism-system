// Tourism System JavaScript
document.addEventListener('DOMContentLoaded', function () {
    initializeSystem();
});

function initializeSystem() {
    // Initialize navigation
    initializeNavigation();

    // Initialize filters
    initializeFilters();

    // Initialize charts (placeholder)
    initializeCharts();

    // Initialize modal handlers
    initializeModals();

    console.log('Tourism System initialized successfully');
}

async function loadDashboardData() {
    try {
        // Use the API controller instead of duplicated methods
        const response = await fetch('/api/dashboardapi/stats');
        const data = await response.json();

        if (data.success) {
            updateDashboardStats(data.data);
        }
    } catch (error) {
        console.error('Error loading dashboard data:', error);
    }
}

async function loadChartData(chartType) {
    try {
        // Use the existing API endpoint
        const response = await fetch(`/api/dashboardapi/chart-data?type=${chartType}`);
        const data = await response.json();

        if (data.success) {
            renderChart(chartType, data.data);
        }
    } catch (error) {
        console.error('Error loading chart data:', error);
    }
}

// Navigation Management
function initializeNavigation() {
    const navItems = document.querySelectorAll('.nav-links a');

    navItems.forEach(item => {
        item.addEventListener('click', (e) => {
            e.preventDefault();
            const targetId = item.getAttribute('href');
            const targetSection = document.querySelector(targetId);

            if (targetSection) {
                // Remove active class from all nav items
                navItems.forEach(nav => nav.classList.remove('active'));
                // Add active class to clicked item
                item.classList.add('active');

                // Smooth scroll to target
                targetSection.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // Mobile menu toggle
    const mobileMenu = document.getElementById('mobileMenu');
    const navLinks = document.getElementById('navLinks');

    if (mobileMenu && navLinks) {
        mobileMenu.addEventListener('click', () => {
            navLinks.classList.toggle('active');
        });
    }
}

// Filter Management
function initializeFilters() {
    // Places filters
    const searchPlace = document.getElementById('searchPlace');
    const categoryFilter = document.getElementById('categoryFilter');
    const statusFilter = document.getElementById('statusFilter');

    if (searchPlace) {
        searchPlace.addEventListener('input', filterPlaces);
    }
    if (categoryFilter) {
        categoryFilter.addEventListener('change', filterPlaces);
    }
    if (statusFilter) {
        statusFilter.addEventListener('change', filterPlaces);
    }

    // Reservations filters
    const dateFrom = document.getElementById('dateFrom');
    const dateTo = document.getElementById('dateTo');
    const reservationStatus = document.getElementById('reservationStatus');

    if (dateFrom) {
        dateFrom.addEventListener('change', filterReservations);
    }
    if (dateTo) {
        dateTo.addEventListener('change', filterReservations);
    }
    if (reservationStatus) {
        reservationStatus.addEventListener('change', filterReservations);
    }
}

function filterPlaces() {
    const searchTerm = document.getElementById('searchPlace')?.value.toLowerCase() || '';
    const categoryFilter = document.getElementById('categoryFilter')?.value || '';
    const statusFilter = document.getElementById('statusFilter')?.value || '';

    const rows = document.querySelectorAll('#placesTable tbody tr');

    rows.forEach(row => {
        const name = row.querySelector('.place-info strong')?.textContent.toLowerCase() || '';
        const category = row.querySelector('.badge')?.textContent.toLowerCase() || '';
        const status = row.querySelector('.status')?.textContent.toLowerCase() || '';

        const matchesSearch = name.includes(searchTerm);
        const matchesCategory = !categoryFilter || category.includes(categoryFilter);
        const matchesStatus = !statusFilter || status.includes(statusFilter);

        if (matchesSearch && matchesCategory && matchesStatus) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });

    updateTableInfo('placesTable', 'Lugares');
}

function filterReservations() {
    const dateFrom = document.getElementById('dateFrom')?.value || '';
    const dateTo = document.getElementById('dateTo')?.value || '';
    const statusFilter = document.getElementById('reservationStatus')?.value || '';

    const rows = document.querySelectorAll('#reservationsTable tbody tr');

    rows.forEach(row => {
        const status = row.querySelector('.status')?.textContent.toLowerCase() || '';
        const matchesStatus = !statusFilter || status.includes(statusFilter);

        // Here you would implement date filtering logic
        // For now, just filter by status

        if (matchesStatus) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });

    updateTableInfo('reservationsTable', 'Reservas');
}

function updateTableInfo(tableId, itemType) {
    const table = document.getElementById(tableId);
    if (!table) return;

    const visibleRows = table.querySelectorAll('tbody tr:not([style*="display: none"])');
    const totalRows = table.querySelectorAll('tbody tr');

    const paginationInfo = table.closest('section')?.querySelector('.pagination-info');
    if (paginationInfo) {
        paginationInfo.textContent = `Mostrando ${visibleRows.length} de ${totalRows.length} ${itemType.toLowerCase()}`;
    }
}

// Charts Initialization (Placeholder)
function initializeCharts() {
    // Category Chart
    const categoryChart = document.getElementById('categoryChart');
    if (categoryChart) {
        const ctx = categoryChart.getContext('2d');
        ctx.fillStyle = '#f8fafc';
        ctx.fillRect(0, 0, categoryChart.width, categoryChart.height);
        ctx.fillStyle = '#6c757d';
        ctx.font = '16px Poppins';
        ctx.textAlign = 'center';
        ctx.fillText('Gráfico de Categorías', categoryChart.width / 2, categoryChart.height / 2);
    }

    // Availability Chart
    const availabilityChart = document.getElementById('availabilityChart');
    if (availabilityChart) {
        const ctx = availabilityChart.getContext('2d');
        ctx.fillStyle = '#f8fafc';
        ctx.fillRect(0, 0, availabilityChart.width, availabilityChart.height);
        ctx.fillStyle = '#6c757d';
        ctx.font = '14px Poppins';
        ctx.textAlign = 'center';
        ctx.fillText('Disponibilidad', availabilityChart.width / 2, availabilityChart.height / 2);
    }

    // History Chart
    const historyChart = document.getElementById('historyChart');
    if (historyChart) {
        const ctx = historyChart.getContext('2d');
        ctx.fillStyle = '#f8fafc';
        ctx.fillRect(0, 0, historyChart.width, historyChart.height);
        ctx.fillStyle = '#6c757d';
        ctx.font = '14px Poppins';
        ctx.textAlign = 'center';
        ctx.fillText('Historial', historyChart.width / 2, historyChart.height / 2);
    }
}

// Modal Management
function initializeModals() {
    // Close modals when clicking outside
    window.addEventListener('click', (e) => {
        if (e.target.classList.contains('modal')) {
            e.target.style.display = 'none';
            document.body.style.overflow = 'auto';
        }
    });

    // Close modals with Escape key
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            const openModal = document.querySelector('.modal[style*="block"]');
            if (openModal) {
                openModal.style.display = 'none';
                document.body.style.overflow = 'auto';
            }
        }
    });
}

// Place Management Functions
function openAddPlaceModal() {
    const modal = document.getElementById('addPlaceModal');
    if (modal) {
        modal.style.display = 'block';
        document.body.style.overflow = 'hidden';

        // Clear form
        const form = document.getElementById('addPlaceForm');
        if (form) {
            form.reset();
        }
    }
}

function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.style.display = 'none';
        document.body.style.overflow = 'auto';
    }
}

function savePlaceForm() {
    const form = document.getElementById('addPlaceForm');
    if (!form) return;

    // Get form data
    const formData = {
        name: document.getElementById('placeName')?.value,
        code: document.getElementById('placeCode')?.value,
        description: document.getElementById('placeDescription')?.value,
        category: document.getElementById('placeCategory')?.value,
        price: document.getElementById('placePrice')?.value,
        capacity: document.getElementById('placeCapacity')?.value,
        location: document.getElementById('placeLocation')?.value
    };

    // Validate required fields
    if (!formData.name || !formData.code || !formData.category || !formData.price) {
        showNotification('Por favor complete todos los campos requeridos', 'error');
        return;
    }

    // Here you would send the data to your backend
    console.log('Saving place:', formData);

    // Simulate API call
    setTimeout(() => {
        showNotification('Lugar guardado exitosamente', 'success');
        closeModal('addPlaceModal');

        // Add new row to table (simulation)
        addPlaceToTable(formData);
    }, 1000);
}

function addPlaceToTable(placeData) {
    const tableBody = document.querySelector('#placesTable tbody');
    if (!tableBody) return;

    const categoryBadges = {
        'alojamiento': 'badge-primary',
        'experiencias': 'badge-success',
        'restaurantes': 'badge-warning',
        'vida-nocturna': 'badge-danger',
        'bodas': 'badge-info'
    };

    const newRow = document.createElement('tr');
    newRow.innerHTML = `
        <td><span class="code">${placeData.code}</span></td>
        <td>
            <div class="place-info">
                <strong>${placeData.name}</strong>
                <small>${placeData.location || 'Ubicación no especificada'}</small>
            </div>
        </td>
        <td><span class="badge ${categoryBadges[placeData.category] || 'badge-primary'}">${placeData.category}</span></td>
        <td><strong>$${placeData.price}</strong></td>
        <td><span class="status status-available">Disponible</span></td>
        <td>
            <div class="availability">
                <span class="available-rooms">${placeData.capacity || 'N/A'}</span>
            </div>
        </td>
        <td>
            <div class="action-buttons">
                <button class="btn-icon btn-edit" onclick="editPlace('${placeData.code}')" title="Editar">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="btn-icon btn-view" onclick="viewPlace('${placeData.code}')" title="Ver detalles">
                    <i class="fas fa-eye"></i>
                </button>
                <button class="btn-icon btn-delete" onclick="deletePlace('${placeData.code}')" title="Eliminar">
                    <i class="fas fa-trash"></i>
                </button>
            </div>
        </td>
    `;

    tableBody.appendChild(newRow);
}

// Place Actions
function editPlace(placeCode) {
    showNotification(`Editando lugar: ${placeCode}`, 'info');
    // Here you would open an edit modal with the place data
}

function viewPlace(placeCode) {
    showNotification(`Viendo detalles del lugar: ${placeCode}`, 'info');
    // Here you would open a view modal with place details
}

function deletePlace(placeCode) {
    if (confirm(`¿Está seguro de que desea eliminar el lugar ${placeCode}?`)) {
        showNotification(`Lugar ${placeCode} eliminado`, 'success');

        // Remove row from table
        const rows = document.querySelectorAll('#placesTable tbody tr');
        rows.forEach(row => {
            const code = row.querySelector('.code')?.textContent;
            if (code === placeCode) {
                row.remove();
            }
        });
    }
}

// Category Management
function openAddCategoryModal() {
    showNotification('Abriendo formulario de nueva categoría', 'info');
    // Here you would open the add category modal
}

function editCategory(categoryId) {
    showNotification(`Editando categoría: ${categoryId}`, 'info');
    // Here you would open edit category modal
}

function deleteCategory(categoryId) {
    if (confirm(`¿Está seguro de que desea eliminar la categoría ${categoryId}?`)) {
        showNotification(`Categoría ${categoryId} eliminada`, 'success');
        // Here you would remove the category
    }
}

// Reservation Management
function openAddReservationModal() {
    showNotification('Abriendo formulario de nueva reserva', 'info');
    // Here you would open the add reservation modal
}

function openCalendarView() {
    showNotification('Abriendo vista de calendario', 'info');
    // Here you would open calendar view
}

function checkIn(reservationId) {
    if (confirm(`¿Confirmar Check-In para la reserva ${reservationId}?`)) {
        showNotification(`Check-In completado para ${reservationId}`, 'success');

        // Update the reservation status in the table
        updateReservationStatus(reservationId, 'En Progreso', 'status-active');
    }
}

function checkOut(reservationId) {
    if (confirm(`¿Confirmar Check-Out para la reserva ${reservationId}?`)) {
        showNotification(`Check-Out completado para ${reservationId}`, 'success');

        // Update the reservation status in the table
        updateReservationStatus(reservationId, 'Completada', 'status-completed');
    }
}

function updateReservationStatus(reservationId, newStatus, newClass) {
    const rows = document.querySelectorAll('#reservationsTable tbody tr');
    rows.forEach(row => {
        const code = row.querySelector('.code')?.textContent;
        if (code === reservationId) {
            const statusElement = row.querySelector('.status');
            if (statusElement) {
                statusElement.className = `status ${newClass}`;
                statusElement.textContent = newStatus;
            }
        }
    });
}

function editReservation(reservationId) {
    showNotification(`Editando reserva: ${reservationId}`, 'info');
    // Here you would open edit reservation modal
}

function viewReservation(reservationId) {
    showNotification(`Viendo detalles de la reserva: ${reservationId}`, 'info');
    // Here you would open view reservation modal
}

// Report Functions
function generateReport() {
    showNotification('Generando reporte completo...', 'info');

    setTimeout(() => {
        showNotification('Reporte generado exitosamente', 'success');
    }, 2000);
}

function exportReport(reportType) {
    showNotification(`Exportando reporte: ${reportType}`, 'info');

    setTimeout(() => {
        showNotification(`Reporte ${reportType} exportado exitosamente`, 'success');
    }, 1500);
}

// Notification System
function showNotification(message, type = 'info') {
    // Remove existing notifications
    const existingNotifications = document.querySelectorAll('.notification');
    existingNotifications.forEach(notification => notification.remove());

    // Create notification element
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
        <div class="notification-content">
            <i class="fas ${getNotificationIcon(type)}"></i>
            <span>${message}</span>
            <button class="notification-close" onclick="this.parentElement.parentElement.remove()">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `;

    // Add notification styles if not already added
    if (!document.querySelector('#notification-styles')) {
        const styles = document.createElement('style');
        styles.id = 'notification-styles';
        styles.textContent = `
            .notification {
                position: fixed;
                top: 20px;
                right: 20px;
                z-index: 10001;
                min-width: 300px;
                max-width: 500px;
                border-radius: 8px;
                box-shadow: 0 4px 12px rgba(0,0,0,0.15);
                animation: slideInRight 0.3s ease;
            }
            
            .notification-content {
                padding: 15px 20px;
                display: flex;
                align-items: center;
                gap: 10px;
                color: white;
            }
            
            .notification-success .notification-content { background: #28a745; }
            .notification-error .notification-content { background: #dc3545; }
            .notification-warning .notification-content { background: #ffc107; color: #333; }
            .notification-info .notification-content { background: #17a2b8; }
            
            .notification-close {
                background: none;
                border: none;
                color: inherit;
                cursor: pointer;
                margin-left: auto;
                padding: 0;
                font-size: 0.9rem;
            }
            
            @keyframes slideInRight {
                from { transform: translateX(100%); opacity: 0; }
                to { transform: translateX(0); opacity: 1; }
            }
        `;
        document.head.appendChild(styles);
    }

    // Add to page
    document.body.appendChild(notification);

    // Auto remove after 5 seconds
    setTimeout(() => {
        if (notification.parentElement) {
            notification.remove();
        }
    }, 5000);
}

function getNotificationIcon(type) {
    const icons = {
        'success': 'fa-check-circle',
        'error': 'fa-exclamation-circle',
        'warning': 'fa-exclamation-triangle',
        'info': 'fa-info-circle'
    };
    return icons[type] || icons.info;
}

// Utility Functions
function formatCurrency(amount) {
    return new Intl.NumberFormat('es-CR', {
        style: 'currency',
        currency: 'CRC'
    }).format(amount);
}

function formatDate(date) {
    return new Intl.DateTimeFormat('es-CR', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    }).format(new Date(date));
}

// Funciones que faltan para evitar errores
function updateDashboardStats(data) {
    console.log('Updating dashboard stats:', data);
}

function renderChart(chartType, data) {
    console.log('Rendering chart:', chartType, data);
}

// Initialize scroll animations
function initializeScrollAnimations() {
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
            }
        });
    }, observerOptions);

    document.querySelectorAll('.fade-in').forEach(el => {
        observer.observe(el);
    });
}

// Initialize on load
window.addEventListener('load', () => {
    initializeScrollAnimations();
});