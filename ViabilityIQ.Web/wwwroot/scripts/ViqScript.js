
// ==========================================================================
// VIABILITY.IQ DASHBOARD ANALYTICS CHARTING ENGINE - CUSTOM INTEROP FUNCTIONS
// ==========================================================================

window.renderViqSalesStockTrends = function (canvasId, salesData, stockData) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    // Destroy prior context handle instance to ensure layout updates draw clean
    if (window.viqTrendChartInstance) { window.viqTrendChartInstance.destroy(); }

    window.viqTrendChartInstance = new Chart(ctx, {
        type: 'line',
        data: {
            labels: ['M1', 'M2', 'M3', 'M4', 'M5', 'M6', 'M7', 'M8', 'M9', 'M10', 'M11', 'M12'],
            datasets: [
                {
                    label: 'Sales Revenue Projection',
                    data: salesData,
                    borderColor: '#1a365d', // ViabilityIQ Corporate Deep Navy Brand Style
                    backgroundColor: 'rgba(26, 54, 93, 0.04)',
                    borderWidth: 2.5,
                    tension: 0.35,
                    fill: true
                },
                {
                    label: 'Stock Value Allocation',
                    data: stockData,
                    borderColor: '#0d9488', // ViabilityIQ Core Vibrant Accent Teal Accent Look
                    backgroundColor: 'transparent',
                    borderWidth: 2,
                    borderDash: [4, 4],
                    tension: 0.1,
                    pointStyle: 'rectRot',
                    pointRadius: 4
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { position: 'top', labels: { boxWidth: 12, font: { size: 10 } } }
            },
            scales: {
                x: { grid: { display: false }, ticks: { font: { size: 9 } } },
                y: { ticks: { font: { size: 9 }, callback: value => '$' + value.toLocaleString() }, grid: { color: '#f1f5f9' } }
            }
        }
    });
};

window.renderViqCashCycleBars = function (canvasId, stockDays, debtorDays, creditorDays, cashCycle) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    if (window.viqBarChartInstance) { window.viqBarChartInstance.destroy(); }

    window.viqBarChartInstance = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: ['Stock Days', 'Debtor Days', 'Creditor Days', 'Cash Cycle'],
            datasets: [{
                data: [stockDays, debtorDays, creditorDays, cashCycle],
                backgroundColor: [
                    '#475569', // Slate Grey - Stock Inventory Frame representation
                    '#3b82f6', // Bright Blue - Debtors Receivable Accounts
                    '#ef4444', // Warning Soft Red - Creditor/Payables Allocation
                    '#0f766e'  // Deep Teal Accent - Complete Realized Liquid Cycle Days
                ],
                borderRadius: 4,
                barThickness: 16
            }]
        },
        options: {
            indexAxis: 'y', // Configures chart rendering horizontally for visual clarity
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false }
            },
            scales: {
                x: { ticks: { font: { size: 9 } }, grid: { color: '#f1f5f9' } },
                y: { ticks: { font: { size: 10, weight: 'bold' } }, grid: { display: false } }
            }
        }
    });
};


