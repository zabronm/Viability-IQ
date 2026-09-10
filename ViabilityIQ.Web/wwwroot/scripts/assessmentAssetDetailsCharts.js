(function () {
    const instances = {};

    function destroyChart(key) {
        if (instances[key]) {
            instances[key].destroy();
            delete instances[key];
        }
    }

    function currency(value) {
        return new Intl.NumberFormat("en-ZA", {
            style: "currency",
            currency: "ZAR",
            maximumFractionDigits: 0
        }).format(value || 0);
    }

    function commonOptions() {
        return {
            responsive: true,
            maintainAspectRatio: false,
            animation: {
                duration: 350
            },
            plugins: {
                legend: {
                    labels: {
                        boxWidth: 10,
                        boxHeight: 10,
                        color: "#5d6c80",
                        font: { size: 10 }
                    }
                },
                tooltip: {
                    callbacks: {
                        label: context => `${context.dataset.label || context.label}: ${currency(context.raw)}`
                    }
                }
            }
        };
    }

    function renderTrend(config) {
        const canvas = document.getElementById(config.canvasId);
        if (!canvas) return;

        destroyChart("trend");
        const options = commonOptions();
        options.plugins.legend.display = false;
        options.scales = {
            x: {
                grid: { display: false },
                ticks: { color: "#7b8899", font: { size: 9 } }
            },
            y: {
                beginAtZero: true,
                grid: { color: "#e4e9ef", borderDash: [3, 4] },
                ticks: {
                    color: "#7b8899",
                    font: { size: 9 },
                    callback: value => currency(value)
                }
            }
        };

        instances.trend = new Chart(canvas, {
            type: "line",
            data: {
                labels: config.labels,
                datasets: [{
                    label: "Closing NBV",
                    data: config.values,
                    borderColor: "#286b9f",
                    backgroundColor: "rgba(40, 107, 159, 0.13)",
                    pointBackgroundColor: "#286b9f",
                    pointBorderColor: "#ffffff",
                    pointBorderWidth: 2,
                    pointRadius: 3,
                    borderWidth: 2,
                    fill: true,
                    tension: 0.28
                }]
            },
            options
        });
    }

    function renderMovements(config) {
        const canvas = document.getElementById(config.canvasId);
        if (!canvas) return;

        destroyChart("movements");
        const options = commonOptions();
        options.plugins.legend.position = "right";

        instances.movements = new Chart(canvas, {
            type: "doughnut",
            data: {
                labels: config.labels,
                datasets: [{
                    data: config.values,
                    backgroundColor: ["#2b76a8", "#cf6572", "#8a75b8", "#5f9f82"],
                    borderColor: "#ffffff",
                    borderWidth: 2,
                    hoverOffset: 6
                }]
            },
            options
        });
    }

    function renderDistribution(config) {
        const canvas = document.getElementById(config.canvasId);
        if (!canvas) return;

        destroyChart("distribution");
        const options = commonOptions();
        options.indexAxis = "y";
        options.plugins.legend.display = false;
        options.scales = {
            x: {
                beginAtZero: true,
                grid: { color: "#e4e9ef" },
                ticks: {
                    color: "#7b8899",
                    font: { size: 8 },
                    callback: value => currency(value)
                }
            },
            y: {
                grid: { display: false },
                ticks: { color: "#66758a", font: { size: 9 } }
            }
        };

        instances.distribution = new Chart(canvas, {
            type: "bar",
            data: {
                labels: config.labels,
                datasets: [{
                    label: "Closing NBV",
                    data: config.values,
                    backgroundColor: "#4c88b2",
                    borderRadius: 4,
                    barThickness: 13
                }]
            },
            options
        });
    }

    function ensurePrintStyles() {
        if (document.getElementById("aad-print-styles")) return;

        const style = document.createElement("style");
        style.id = "aad-print-styles";
        style.textContent = `
            @page {
                size: landscape;
                margin: 8mm;
            }

            @media print {
                html,
                body.aad-printing {
                    width: 100% !important;
                    height: auto !important;
                    min-height: 0 !important;
                    overflow: visible !important;
                    background: #fff !important;
                }

                body.aad-printing .fa-top-bar,
                body.aad-printing .fa-sidebar,
                body.aad-printing .offcanvas,
                body.aad-printing .offcanvas-backdrop,
                body.aad-printing .toast-container {
                    display: none !important;
                }

                body.aad-printing .fa-layout-wrapper,
                body.aad-printing .fa-workspace-container,
                body.aad-printing .fa-main-content-surface,
                body.aad-printing .content-body-padding {
                    display: block !important;
                    position: static !important;
                    width: 100% !important;
                    height: auto !important;
                    min-height: 0 !important;
                    max-height: none !important;
                    margin: 0 !important;
                    padding: 0 !important;
                    overflow: visible !important;
                }

                body.aad-printing .aad-page {
                    display: block !important;
                    position: static !important;
                    width: 100% !important;
                    height: auto !important;
                    min-height: 0 !important;
                    max-height: none !important;
                    margin: 0 !important;
                    padding: 0 !important;
                    overflow: visible !important;
                    background: #fff !important;
                }

                body.aad-printing .aad-alert-panel,
                body.aad-printing .aad-metrics-panel,
                body.aad-printing .aad-chart-card {
                    break-inside: avoid;
                    page-break-inside: avoid;
                    box-shadow: none !important;
                }

                body.aad-printing .aad-movement-panel {
                    break-inside: auto !important;
                    page-break-inside: auto !important;
                    overflow: visible !important;
                }

                body.aad-printing .aad-alert-toggle,
                body.aad-printing .aad-toolbar-actions,
                body.aad-printing .aad-legend,
                body.aad-printing .aad-footnote,
                body.aad-printing .aad-actions-column,
                body.aad-printing .aad-actions-cell,
                body.aad-printing .aad-detail-actions-spacer,
                body.aad-printing button {
                    display: none !important;
                }

                body.aad-printing .aad-table-frame {
                    overflow: visible !important;
                }

                body.aad-printing .aad-movement-table {
                    width: 100% !important;
                    min-width: 0 !important;
                    table-layout: fixed !important;
                }

                body.aad-printing .aad-movement-table thead {
                    display: table-header-group !important;
                }

                body.aad-printing .aad-movement-table tfoot {
                    display: table-footer-group !important;
                }

                body.aad-printing .aad-movement-table th,
                body.aad-printing .aad-movement-table td {
                    padding: 2.5px 2px !important;
                    font-size: 6.5pt !important;
                }

                body.aad-printing .aad-movement-table .aad-asset-column {
                    width: 16% !important;
                }

                body.aad-printing .aad-movement-table .aad-month-column {
                    width: 7% !important;
                }

                body.aad-printing .aad-asset-group {
                    break-inside: avoid;
                    page-break-inside: avoid;
                }

                body.aad-printing .aad-analytics-panel {
                    break-before: page;
                    page-break-before: always;
                    overflow: visible !important;
                }

                body.aad-printing .aad-chart-grid {
                    grid-template-columns: 1.5fr 1fr !important;
                    overflow: visible !important;
                }

                body.aad-printing .aad-chart-canvas-wide {
                    height: 260px !important;
                }

                body.aad-printing .aad-chart-canvas {
                    height: 180px !important;
                }

                body.aad-printing canvas {
                    max-width: 100% !important;
                }
            }
        `;
        document.head.appendChild(style);
    }

    function printPage(mode) {
        ensurePrintStyles();

        document.body.classList.add("aad-printing");
        document.body.classList.toggle("aad-print-detailed", mode === "detailed");
        document.body.classList.toggle("aad-print-summary", mode !== "detailed");

        return new Promise(resolve => {
            let completed = false;

            const cleanup = () => {
                if (completed) return;
                completed = true;
                document.body.classList.remove(
                    "aad-printing",
                    "aad-print-detailed",
                    "aad-print-summary");
                resolve();
            };

            window.addEventListener("afterprint", cleanup, { once: true });
            window.setTimeout(cleanup, 60000);
            window.requestAnimationFrame(() => window.print());
        });
    }

    window.viqAssetDetailsCharts = {
        render: function (payload) {
            if (typeof Chart === "undefined") {
                throw new Error("Chart.js must be loaded before assessmentAssetDetailsCharts.js.");
            }

            renderTrend(payload.trend);
            renderMovements(payload.movements);
            renderDistribution(payload.distribution);
        },
        print: function (mode) {
            return printPage(mode);
        },
        destroy: function () {
            destroyChart("trend");
            destroyChart("movements");
            destroyChart("distribution");
        }
    };
})();
