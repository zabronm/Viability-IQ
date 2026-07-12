
//window.renderDebtorsPieChart = (data) => {
//    const ctx = document.getElementById('debtorsChart').getContext('2d');
//    if (window.debtorsChartInstance) window.debtorsChartInstance.destroy();

//    window.debtorsChartInstance = new Chart(ctx, {
//        type: 'pie',
//        data: {
//            labels: ['0-30', '30-60', '60-90', '90-120', '120+'],
//            datasets: [{
//                data: data,
//                backgroundColor: ['#0d6efd', '#0dcaf0', '#ffc107', '#fd7e14', '#dc3545'],
//                hoverOffset: 25,
//                offset: 10
//            }]
//        },
//        options: {
//            responsive: true,
//            plugins: {
//                legend: {
//                    position: 'right', // Moves legend to the right
//                    align: 'center',
//                    labels: {
//                        boxWidth: 10,  // Reduces the width of color indicators
//                        boxHeight: 10, // Adjusts height for a cleaner look
//                        padding: 15
//                    }
//                }
//            }
//        }
//    });
//}


window.renderDebtorsPieChart = (data) => {
    const ctx = document.getElementById('debtorsChart').getContext('2d');
    if (window.debtorsChartInstance) window.debtorsChartInstance.destroy();

    // Calculate total for percentage calculation
    const total = data.reduce((a, b) => a + b, 0);

    window.debtorsChartInstance = new Chart(ctx, {
        type: 'pie',
        data: {
            labels: ['0-30', '30-60', '60-90', '90-120', '120+'],
            datasets: [{
                data: data,
                backgroundColor: ['#0d6efd', '#0dcaf0', '#ffc107', '#fd7e14', '#dc3545'],
                hoverOffset: 25,
                offset: 10
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'right',
                    labels: { boxWidth: 10, padding: 10 }
                },
                // Custom datalabels plugin configuration
                datalabels: {
                    formatter: (value, ctx) => {
                        let percentage = ((value * 100) / total).toFixed(0) + "%";
                        let currency = "R " + value.toLocaleString();
                        return percentage + "\n" + currency;
                    },
                    color: '#fff',
                    font: { weight: 'bold', size: 10 }
                }
            }
        }
    });
};