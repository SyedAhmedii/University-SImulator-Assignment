// calculator.js
$(document).ready(function () {
    const $modelSelect = $('#Model');
    const $modelPills = $('.model-pill');
    const $serversGroup = $('#servers-group');
    const $arrivalDistBadge = $('#arrival-dist-badge');
    const $serviceDistBadge = $('#service-dist-badge');
    const $arrivalDistSelect = $('#ArrivalDistribution');
    const $serviceDistSelect = $('#ServiceDistribution');
    
    // Arrival specific containers
    const $arrivalUniformGroup = $('#arrival-uniform-group');
    const $arrivalNormalGroup = $('#arrival-normal-group');
    // Service specific containers
    const $serviceUniformGroup = $('#service-uniform-group');
    const $serviceNormalGroup = $('#service-normal-group');

    // Live preview elements
    const $lambdaText = $('#preview-lambda');
    const $muText = $('#preview-mu');
    const $rhoText = $('#preview-rho');
    const $rhoContainer = $('#rho-container');

    // Inputs for calculation
    const $arrivalInputMode = $('input[name="ArrivalInputMode"]');
    const $arrivalValue = $('#ArrivalValue');
    const $serviceInputMode = $('input[name="ServiceInputMode"]');
    const $serviceValue = $('#ServiceValue');
    const $servers = $('#Servers');

    // Init
    updateUI();

    // Event Listeners
    $modelPills.on('click', function () {
        const val = $(this).data('model');
        $modelSelect.val(val);
        $modelPills.removeClass('active');
        $(this).addClass('active');
        
        // Update URL query string without reload
        const url = new URL(window.location);
        url.searchParams.set('model', val);
        window.history.pushState({}, '', url);
        
        updateUI();
    });

    $('#ArrivalDistribution, #ServiceDistribution').on('change', updateUI);
    $('input[name="UseVarianceForArrival"], input[name="UseVarianceForService"]').on('change', updateUI);
    
    // Live preview recalculation
    $arrivalInputMode.on('change', recalculatePreview);
    $serviceInputMode.on('change', recalculatePreview);
    $arrivalValue.on('input', recalculatePreview);
    $serviceValue.on('input', recalculatePreview);
    $servers.on('input', recalculatePreview);

    // Auto-compute Variance/StdDev for Normal
    $('#ArrivalStdDev').on('input', function() {
        if(!$('#UseVarianceForArrival_True').is(':checked')){
            let val = parseFloat($(this).val());
            if(!isNaN(val)) $('#ArrivalVariance').val((val*val).toFixed(4));
        }
    });
    $('#ArrivalVariance').on('input', function() {
        if($('#UseVarianceForArrival_True').is(':checked')){
            let val = parseFloat($(this).val());
            if(!isNaN(val) && val >= 0) $('#ArrivalStdDev').val(Math.sqrt(val).toFixed(4));
        }
    });
    $('#ServiceStdDev').on('input', function() {
        if(!$('#UseVarianceForService_True').is(':checked')){
            let val = parseFloat($(this).val());
            if(!isNaN(val)) $('#ServiceVariance').val((val*val).toFixed(4));
        }
    });
    $('#ServiceVariance').on('input', function() {
        if($('#UseVarianceForService_True').is(':checked')){
            let val = parseFloat($(this).val());
            if(!isNaN(val) && val >= 0) $('#ServiceStdDev').val(Math.sqrt(val).toFixed(4));
        }
    });

    function updateUI() {
        const model = $modelSelect.val();
        
        // 1. Show/Hide Servers input
        if (model === 'MMS' || model === 'MGS' || model === 'GGS') {
            $serversGroup.show();
        } else {
            $serversGroup.hide();
            $servers.val(1);
        }

        // 2. Setup Distributions based on model
        // Arrival
        if (model.startsWith('M')) {
            $arrivalDistBadge.show();
            $arrivalDistSelect.hide();
            $arrivalUniformGroup.hide();
            $arrivalNormalGroup.hide();
        } else {
            $arrivalDistBadge.hide();
            $arrivalDistSelect.show();
            let dist = $arrivalDistSelect.val();
            $arrivalUniformGroup.toggle(dist === 'Uniform');
            $arrivalNormalGroup.toggle(dist === 'Normal');
        }

        // Service
        if (model === 'MM1' || model === 'MMS') {
            $serviceDistBadge.show();
            $serviceDistSelect.hide();
            $serviceUniformGroup.hide();
            $serviceNormalGroup.hide();
        } else {
            $serviceDistBadge.hide();
            $serviceDistSelect.show();
            let dist = $serviceDistSelect.val();
            $serviceUniformGroup.toggle(dist === 'Uniform');
            $serviceNormalGroup.toggle(dist === 'Normal');
        }

        // Normal dist variance/stddev toggles
        const useArrVar = $('input[name="UseVarianceForArrival"]:checked').val() === 'True';
        $('#ArrivalStdDev').prop('readonly', useArrVar);
        $('#ArrivalVariance').prop('readonly', !useArrVar);

        const useSrvVar = $('input[name="UseVarianceForService"]:checked').val() === 'True';
        $('#ServiceStdDev').prop('readonly', useSrvVar);
        $('#ServiceVariance').prop('readonly', !useSrvVar);

        recalculatePreview();
    }

    function recalculatePreview() {
        const arrMode = $('input[name="ArrivalInputMode"]:checked').val();
        const srvMode = $('input[name="ServiceInputMode"]:checked').val();
        const arrVal = parseFloat($arrivalValue.val());
        const srvVal = parseFloat($serviceValue.val());
        const s = parseFloat($servers.val()) || 1;
        const actualS = ($modelSelect.val() === 'MM1' || $modelSelect.val() === 'MG1' || $modelSelect.val() === 'GG1') ? 1 : s;

        let lambda = 0, mu = 0, rho = 0;

        if (!isNaN(arrVal) && arrVal > 0) {
            lambda = arrMode === 'MeanTime' ? (1 / arrVal) : arrVal;
            $lambdaText.text(lambda.toFixed(4));
        } else {
            $lambdaText.text('0.0000');
        }

        if (!isNaN(srvVal) && srvVal > 0) {
            mu = srvMode === 'MeanTime' ? (1 / srvVal) : srvVal;
            $muText.text(mu.toFixed(4));
        } else {
            $muText.text('0.0000');
        }

        if (lambda > 0 && mu > 0) {
            rho = lambda / (actualS * mu);
            $rhoText.text(rho.toFixed(4));
            
            if (rho >= 1) {
                $rhoContainer.addClass('bg-danger bg-opacity-10 text-danger border-danger');
                $rhoText.addClass('text-danger');
            } else {
                $rhoContainer.removeClass('bg-danger bg-opacity-10 text-danger border-danger');
                $rhoText.removeClass('text-danger');
            }
        } else {
            $rhoText.text('0.0000');
            $rhoContainer.removeClass('bg-danger bg-opacity-10 text-danger border-danger');
            $rhoText.removeClass('text-danger');
        }
    }
});
