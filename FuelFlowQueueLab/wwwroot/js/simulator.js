// simulator.js
$(document).ready(function () {
    const $arrDistCards = $('.arr-dist-card');
    const $srvDistCards = $('.srv-dist-card');
    const $arrDistInput = $('#ArrivalDistribution');
    const $srvDistInput = $('#ServiceDistribution');
    const $detectedModelBadge = $('#detected-model');

    // UI Updates
    function updateSimulatorUI() {
        // Arrival Distribution Cards
        const arrDist = $arrDistInput.val();
        $arrDistCards.removeClass('selected border-amber-500 bg-amber-50');
        $arrDistCards.filter(`[data-dist="${arrDist}"]`).addClass('selected border-amber-500 bg-amber-50');

        $('#arr-uniform-params').toggle(arrDist === 'Uniform');
        $('#arr-normal-params').toggle(arrDist === 'Normal');

        // Service Distribution Cards
        const srvDist = $srvDistInput.val();
        $srvDistCards.removeClass('selected border-amber-500 bg-amber-50');
        $srvDistCards.filter(`[data-dist="${srvDist}"]`).addClass('selected border-amber-500 bg-amber-50');

        $('#srv-uniform-params').toggle(srvDist === 'Uniform');
        $('#srv-normal-params').toggle(srvDist === 'Normal');
        $('#srv-gamma-params').toggle(srvDist === 'Gamma');

        // Detected Model
        let s = parseInt($('#Servers').val()) || 1;
        let arrM = arrDist === 'Exponential';
        let srvM = srvDist === 'Exponential';
        
        let detected = '';
        if (arrM && srvM && s === 1) detected = 'M/M/1';
        else if (arrM && srvM && s > 1) detected = 'M/M/s';
        else if (arrM && !srvM && s === 1) detected = 'M/G/1';
        else if (arrM && !srvM && s > 1) detected = 'M/G/s';
        else if (!arrM && s === 1) detected = 'G/G/1';
        else detected = 'G/G/s';

        $detectedModelBadge.text(detected);
        
        recalculatePreview();
    }

    // Event Listeners
    $arrDistCards.on('click', function() {
        $arrDistInput.val($(this).data('dist'));
        updateSimulatorUI();
    });

    $srvDistCards.on('click', function() {
        $srvDistInput.val($(this).data('dist'));
        updateSimulatorUI();
    });

    $('#Servers').on('input', updateSimulatorUI);

    $('input[name="ArrivalInputMode"]').on('change', recalculatePreview);
    $('input[name="ServiceInputMode"]').on('change', recalculatePreview);
    $('#ArrivalValue').on('input', recalculatePreview);
    $('#ServiceValue').on('input', recalculatePreview);

    function recalculatePreview() {
        const arrMode = $('input[name="ArrivalInputMode"]:checked').val();
        const srvMode = $('input[name="ServiceInputMode"]:checked').val();
        const arrVal = parseFloat($('#ArrivalValue').val());
        const srvVal = parseFloat($('#ServiceValue').val());
        const s = parseFloat($('#Servers').val()) || 1;

        let lambda = 0, mu = 0;

        if (!isNaN(arrVal) && arrVal > 0) {
            lambda = arrMode === 'MeanTime' ? (1 / arrVal) : arrVal;
            $('#sim-lambda-preview').text(`λ = ${lambda.toFixed(4)} / min`);
        }

        if (!isNaN(srvVal) && srvVal > 0) {
            mu = srvMode === 'MeanTime' ? (1 / srvVal) : srvVal;
            let rho = lambda / (s * mu);
            $('#sim-mu-preview').text(`μ = ${mu.toFixed(4)} / min | ρ = ${rho.toFixed(4)}`);
        }
    }

    // Init
    updateSimulatorUI();

    // Chart.js rendering (if data exists on page)
    if (typeof chartQueueTimes !== 'undefined' && chartQueueTimes.length > 0) {
        
        // 1. Queue Length Over Time
        const ctxQueue = document.getElementById('queueLengthChart');
        if (ctxQueue) {
            new Chart(ctxQueue, {
                type: 'line',
                data: {
                    labels: chartQueueTimes.map(t => t.toFixed(2)),
                    datasets: [{
                        label: 'Queue Length',
                        data: chartQueueLengths,
                        borderColor: '#f59e0b', // amber-500
                        backgroundColor: 'rgba(245, 158, 11, 0.1)',
                        borderWidth: 2,
                        stepped: true,
                        fill: true,
                        pointRadius: 0
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        x: { title: { display: true, text: 'Time' } },
                        y: { title: { display: true, text: 'Customers in Queue' }, beginAtZero: true, ticks: { stepSize: 1 } }
                    }
                }
            });
        }

        // 2. Waiting Time per Customer
        const ctxWait = document.getElementById('waitTimeChart');
        if (ctxWait) {
            new Chart(ctxWait, {
                type: 'bar',
                data: {
                    labels: chartWaitTimes.map((_, i) => `Cust ${i+1}`),
                    datasets: [{
                        label: 'Wait Time in Queue',
                        data: chartWaitTimes,
                        backgroundColor: '#1a2744', // navy-700
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        y: { title: { display: true, text: 'Wait Time' }, beginAtZero: true }
                    }
                }
            });
        }

        // 3. Server Utilization (if s > 1)
        const ctxUtil = document.getElementById('utilizationChart');
        if (ctxUtil && typeof serverUtilizations !== 'undefined') {
            new Chart(ctxUtil, {
                type: 'bar',
                data: {
                    labels: serverUtilizations.map((_, i) => `Server ${i+1}`),
                    datasets: [{
                        label: 'Utilization %',
                        data: serverUtilizations,
                        backgroundColor: '#10b981', // emerald
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        y: { beginAtZero: true, max: 100 }
                    }
                }
            });
        }
    }

    // CSV Export
    $('#btnExportCsv').on('click', function() {
        let csvContent = "data:text/csv;charset=utf-8,";
        let rows = document.querySelectorAll("#customersTable tr");
        
        for (let i = 0; i < rows.length; i++) {
            let row = [], cols = rows[i].querySelectorAll("td, th");
            for (let j = 0; j < cols.length; j++) {
                row.push(cols[j].innerText.replace(/,/g, ""));
            }
            csvContent += row.join(",") + "\r\n";
        }

        const encodedUri = encodeURI(csvContent);
        const link = document.createElement("a");
        link.setAttribute("href", encodedUri);
        link.setAttribute("download", "simulation_results.csv");
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    });
});
