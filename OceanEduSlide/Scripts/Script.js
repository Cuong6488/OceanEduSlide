AOS.init({
    offset: 200,
    once: true,
    duration: 500
});

var isTick = false;
function login() {
    $(".login-box a").on("click", function () {
        $(".first-login").fadeOut(300, function () { // Ẩn phần tử với hiệu ứng mờ dần
            $(".second-login").fadeIn(300); // Hiển thị phần tử với hiệu ứng mờ dần
        });
    });
}

$("[data-fancybox]").fancybox({
    zoomOpacity: "auto",
    zoomSpeed: 11000,
    wheel: "auto", // Cho phép phóng to bằng cuộn chuột
    clickSlide: "zoom", // Cho phép phóng to bằng cách nhấp vào ảnh
    maxScale: 2,
    fitToView: false,
});

$(document).ready(function () {
    $.datepicker.regional['vi'] = {
        closeText: 'Đóng',
        prevText: '&#x3C;Trước',
        nextText: 'Tiếp&#x3E;',
        currentText: 'Hôm nay',
        monthNames: ['Tháng Một', 'Tháng Hai', 'Tháng Ba', 'Tháng Tư',
            'Tháng Năm', 'Tháng Sáu', 'Tháng Bảy', 'Tháng Tám', 'Tháng Chín',
            'Tháng Mười', 'Tháng Mười Một', 'Tháng Mười Hai'],
        monthNamesShort: ['Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4',
            'Tháng 5', 'Tháng 6', 'Tháng 7', 'Tháng 8', 'Tháng 9', 'Tháng 10',
            'Tháng 11', 'Tháng 12'],
        dayNames: ['Chủ Nhật', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm',
            'Thứ Sáu', 'Thứ Bảy'],
        dayNamesShort: ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'],
        dayNamesMin: ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'],
        weekHeader: 'Tu', dateFormat: 'dd/mm/yy',
        firstDay: 1,
        isRTL: false,
        showMonthAfterYear: false,
        yearSuffix: ''
    };
    $.datepicker.setDefaults($.datepicker.regional['vi']);

});
function price() {

    $('.input-container-qdsale select').select2({allowClear: true });
    var today = new Date();
    $(".datepicker").datepicker({
        dateFormat: "dd/mm/yy",
        yearRange: "1900:2100",
        minDate: today,
    });
    $(".input-container-paychanel select").on("change", function (data) {
        const paychanelVal = $(this).val();
        var items = [];
        items.push("<option value>Chọn kỳ hạn</option>");

        if (paychanelVal !== "") {
            if (paychanelVal === "Lotte") {
                items.push('<option value="6">6 tháng</option>');
                items.push('<option value="9">9 tháng</option>');
                items.push('<option value="12">12 tháng</option>');
            }
            else if (paychanelVal === "MSB" || paychanelVal === "Sacombank") {
                items.push('<option value="6">6 tháng</option>');
                items.push('<option value="9">9 tháng</option>');
                items.push('<option value="12">12 tháng</option>');
                items.push('<option value="18">18 tháng</option>');
                items.push('<option value="24">24 tháng</option>');
            }
            else if (paychanelVal === "VPbank") {
                items.push('<option value="3">3 tháng</option>');
                items.push('<option value="6">6 tháng</option>');
                items.push('<option value="9">9 tháng</option>');
                items.push('<option value="12">12 tháng</option>');
            }
            $(this).closest(".price-advice-box").find(".input-container-term").find("select").html(items.join(""));

        }
        else {
            $(this).closest(".price-advice-box").find(".input-container-term").find("select").html(items.join(""));
        }
    });
    $(".input-container-cth select").on("change", function (data) {
        $(this).closest(".price-advice-box").find(".input-container-qdsale").find("select").prop("selectedIndex", 0);
        $(this).closest(".price-advice-box").find(".input-container-moneyprice").find("input").val("");
        $(this).closest(".price-advice-box").find(".input-container-giftprice").find("input").val("");
        $(this).closest(".price-advice-box").find(".input-container-finalprice").find("input").val("");
        $(this).closest(".price-advice-box").find(".input-container-paymethod").find("input").val("");
        $(this).closest(".price-advice-box").find(".input-container-prepay").find("input").val("");
        $(this).closest(".price-advice-box").find(".input-container-postpaid").find("input").val("");
        $(this).closest(".price-advice-box").find(".input-container-unitprice").find("input").val("");
        $(this).closest(".price-advice-box").find(".input-container-totalprice").find("input").val("");
        $(this).closest(".price-advice-box").find(".input-container-pricepermonth").find("input").val("");
        $(this).closest(".price-advice-box").find(".input-container-pathway").find("input").val("");
        $(this).closest(".price-advice-box").find(".start-date").val("");
        $(this).closest(".price-advice-box").find(".end-date").val("");
        $(this).closest(".price-advice-box").find(".input-container-term").find("select").prop("selectedIndex", 0);
        const id = $(this).val();
        var items = [];
        items.push("<option value>Chọn cấp độ học</option>");
        if (id !== "") {
            if (id === "Anh văn nhi đồng") {
                items.push('<option value="no-ielts-48">PRE-KINDY A</option>');
                items.push('<option value="no-ielts-48">PRE-KINDY B</option>');
                items.push('<option value="no-ielts-48">KINDY1A</option>');
                items.push('<option value="no-ielts-48">KINDY1B</option>');
                items.push('<option value="no-ielts-48">KINDY2A</option>');
                items.push('<option value="no-ielts-48">KINDY2B</option>');
                items.push('<option value="no-ielts-48">KINDY3A</option>');
                items.push('<option value="no-ielts-48">KINDY3B</option>');
            }
            else if (id === "Anh văn thiếu nhi") {
                items.push('<option value="no-ielts-48">PRE-KIDS A</option>');
                items.push('<option value="no-ielts-48">PRE-KIDS B</option>');
                items.push('<option value="no-ielts-48">PRE - KINDY B</option>');
                items.push('<option value="no-ielts-48">KIDS1A</option>');
                items.push('<option value="no-ielts-48">KIDS1B</option>');
                items.push('<option value="no-ielts-48">KIDS2A</option>');
                items.push('<option value="no-ielts-48">KIDS2B</option>');
                items.push('<option value="no-ielts-48">KIDS3A</option>');
                items.push('<option value="no-ielts-48">KIDS3B</option>');
                items.push('<option value="no-ielts-48">KIDS4A</option>');
                items.push('<option value="no-ielts-48">KIDS4B</option>');
                items.push('<option value="no-ielts-48">KIDS5A</option>');
                items.push('<option value="no-ielts-48">KIDS5B</option>');
            }
            else if (id === "T.A học thuật Trung học") {
                items.push('<option value="no-ielts-72">TEENS1A</option>');
                items.push('<option value="no-ielts-72">TEENS1B</option>');
                items.push('<option value="no-ielts-72">TEENS2A</option>');
                items.push('<option value="no-ielts-72">TEENS2B</option>');
                items.push('<option value="ielts-72">PRE-ielts</option>');
                items.push('<option value="ielts-72">ielts 4.0</option>');
                items.push('<option value="ielts-72">ielts 4.5</option>');
                items.push('<option value="ielts-72">ielts 5.0</option>');
                items.push('<option value="ielts-72">ielts 5.5</option>');
                items.push('<option value="ielts-72">ielts 6.0</option>');
            }
            else if (id === "Luyện thi IELTS") {
                items.push('<option value="no-ielts-48">GN1A</option>');
                items.push('<option value="no-ielts-48">GN1B</option>');
                items.push('<option value="no-ielts-48">GN2A</option>');
                items.push('<option value="no-ielts-48">GN2B</option>');
                items.push('<option value="ielts-72">PRE-IELTS</option>');
                items.push('<option value="ielts-72">IELTS 4.0</option>');
                items.push('<option value="ielts-72">IELTS 4.5</option>');
                items.push('<option value="ielts-72">IELTS 5.0</option>');
                items.push('<option value="ielts-72">IELTS 5.5</option>');
                items.push('<option value="ielts-72">IELTS 6.0</option>');
                items.push('<option value="ielts-72">IELTS 6.5</option>');
                items.push('<option value="ielts-72">IELTS 7.0</option>');
                items.push('<option value="ielts-72">IELTS 7.5</option>');
                items.push('<option value="ielts-72">IELTS 8.0</option>');
                items.push('<option value="ielts-72">IELTS 8.5</option>');
            }
            else if (id === "T.A giao tiếp quốc tế TOEIC") {
                items.push('<option value="no-ielts-48">GN1A</option>');
                items.push('<option value="no-ielts-48">GN1B</option>');
                items.push('<option value="no-ielts-48">GN2A</option>');
                items.push('<option value="no-ielts-48">GN2B</option>');
                items.push('<option value="no-ielts-48">TOEIC 400</option>');
                items.push('<option value="no-ielts-48">TOEIC 450</option>');
                items.push('<option value="no-ielts-48">TOEIC 500</option>');
                items.push('<option value="no-ielts-48">TOEIC 550</option>');
                items.push('<option value="no-ielts-48">TOEIC 600</option>');
                items.push('<option value="no-ielts-48">TOEIC 650</option>');
                items.push('<option value="no-ielts-48">TOEIC 700</option>');
                items.push('<option value="no-ielts-48">TOEIC 750</option>');
                items.push('<option value="no-ielts-48">TOEIC 800</option>');
                items.push('<option value="no-ielts-48">TOEIC 850</option>');
                items.push('<option value="no-ielts-48">TOEIC 900</option>');
                items.push('<option value="no-ielts-48">TOEIC 950</option>');
            }
            $(this).closest(".input-container-cth").siblings(".input-container-level").find("select").html(items.join(""));

        }
        else {
            $(this).closest(".input-container-cth").siblings(".input-container-level").find("select").html(items.join(""));
        }

    });
    $(document).ready(function () {
        // Hàm chuyển đổi từ chuỗi ngày dạng "dd/mm/yy" thành đối tượng Date
        function parseDate(dateStr) {
            const parts = dateStr.split("/");
            return new Date(parts[2], parts[1] - 1, parts[0]); // Năm, Tháng (0-based), Ngày
        }

        // Hàm chuyển đổi từ đối tượng Date thành chuỗi ngày dạng "dd/mm/yy"
        function formatDate(date) {
            const day = String(date.getDate()).padStart(2, "0");
            const month = String(date.getMonth() + 1).padStart(2, "0"); // Tháng bắt đầu từ 0
            const year = date.getFullYear();
            return `${day}/${month}/${year}`;
        }

        // Hàm tính toán đơn giá
        function calculateUnitPrice(levelValue) {
            if (levelValue.includes("no-ielts")) {
                return 2989000; // Đơn giá cho cấp độ có chứa "no-ielts"
            }
            else if (levelValue.includes("ielts")) {
                return 3989000; // Đơn giá cho cấp độ "ielts"
            }

            //if (levelValue === "ielts") {
            //    return 3989000; // Đơn giá cho cấp độ "ielts"
            //}
            //else if (levelValue === "no-ielts") {
            //    return 2989000; // Đơn giá cho cấp độ "no-ielts"
            //}
            else {
                return ""; // Đơn giá rỗng nếu không có giá trị
            }
        }

        // Hàm tính toán thành tiền
        function calculateTotalPrice(unitPrice, pathwayMonths) {
            if (unitPrice && pathwayMonths) {
                return unitPrice * pathwayMonths; // Thành tiền = đơn giá * số tháng
            }
            else {
                return ""; // Thành tiền rỗng nếu thiếu giá trị
            }
        }

        // Khi thay đổi thời gian bắt đầu
        $(".start-date").on("change", function () {
            //const startDateValue = $(this).val();
            //const pathwayMonths = $(this).closest(".price-advice-box").find(".input-container-pathway").find("input").val();

            //if (startDateValue && pathwayMonths) {
            //    const startDate = parseDate(startDateValue);
            //    startDate.setMonth(startDate.getMonth() + parseInt(pathwayMonths));
            //    $(this).closest(".price-advice-box").find(".end-date").val(formatDate(startDate)); // Cập nhật thời gian kết thúc
            //} else {
            //    $(this).closest(".price-advice-box").find(".end-date").val("");
            //}
            const pathwayMonths = parseFloat($(this).closest(".price-advice-box").find(".input-container-pathway").find("input").val());
            const startDateValue = $(this).val();

            if (startDateValue && !isNaN(pathwayMonths)) {
                const startDate = parseDate(startDateValue);

                // Lấy phần nguyên và phần thập phân của số tháng
                const wholeMonths = Math.floor(pathwayMonths);
                const fractionalMonths = pathwayMonths - wholeMonths;

                // Thêm tháng
                startDate.setMonth(startDate.getMonth() + wholeMonths);

                // Thêm ngày từ phần thập phân (giả sử 1 tháng = 30 ngày)
                const extraDays = Math.round(fractionalMonths * 30);
                startDate.setDate(startDate.getDate() + extraDays);

                $(this).closest(".price-advice-box").find(".end-date").val(formatDate(startDate)); // Cập nhật thời gian kết thúc
            } else {
                $(this).closest(".price-advice-box").find(".end-date").val(""); // Đặt thời gian kết thúc về chuỗi rỗng
            }
        });

        //Khi thay đổi lộ trình
        $(".input-container-pathway input").on("change", function () {
            $(this).closest(".price-advice-box").find(".input-container-qdsale").find("select").prop("selectedIndex", 0);
            $(this).closest(".price-advice-box").find(".input-container-moneyprice").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-giftprice").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-finalprice").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-paymethod").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-prepay").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-postpaid").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-pricepermonth").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-term").find("select").prop("selectedIndex", 0);
            //const pathwayMonths = $(this).val();
            //const startDateValue = $(this).closest(".price-advice-box").find(".start-date").val();

            //if (startDateValue && pathwayMonths) {
            //    const startDate = parseDate(startDateValue);
            //    startDate.setMonth(startDate.getMonth() + parseInt(pathwayMonths));
            //    $(this).closest(".price-advice-box").find(".end-date").val(formatDate(startDate)); // Cập nhật thời gian kết thúc
            //} else {
            //    $(this).closest(".price-advice-box").find(".end-date").val(""); // Đặt thời gian kết thúc về chuỗi rỗng
            //}
            const pathwayMonths = parseFloat($(this).val());
            const startDateValue = $(this).closest(".price-advice-box").find(".start-date").val();

            if (startDateValue && !isNaN(pathwayMonths)) {
                const startDate = parseDate(startDateValue);

                // Lấy phần nguyên và phần thập phân của số tháng
                const wholeMonths = Math.floor(pathwayMonths);
                const fractionalMonths = pathwayMonths - wholeMonths;

                // Thêm tháng
                startDate.setMonth(startDate.getMonth() + wholeMonths);

                // Thêm ngày từ phần thập phân (giả sử 1 tháng = 30 ngày)
                const extraDays = Math.round(fractionalMonths * 30);
                startDate.setDate(startDate.getDate() + extraDays);

                $(this).closest(".price-advice-box").find(".end-date").val(formatDate(startDate)); // Cập nhật thời gian kết thúc
            } else {
                $(this).closest(".price-advice-box").find(".end-date").val(""); // Đặt thời gian kết thúc về chuỗi rỗng
            }

            const levelValue2 = $(this).closest(".input-container-pathway").siblings(".input-container-level").find("select").val(); // Cấp độ học

            const pathwayMonths2 = $(this).val(); // Lộ trình

            const unitPrice2 = calculateUnitPrice(levelValue2); // Tính đơn giá
            const totalPrice2 = calculateTotalPrice(unitPrice2, pathwayMonths2); // Tính thành tiền
            //Hiển thị thành tiền
            $(this).closest(".input-container-pathway").siblings(".input-container-totalprice").find("input").val(totalPrice2 ? `${totalPrice2.toLocaleString()}đ` : "");
            $(this).closest(".input-container-pathway").siblings(".input-container-finalprice").find("input").val(totalPrice2 ? `${totalPrice2.toLocaleString()}đ` : "");
            if ($(this).closest(".price-advice-box").find(".input-container-finalprice").find("input").val() !== "") {
                var finalMoney = $(this).closest(".price-advice-box").find(".input-container-finalprice").find("input").val().replace(/\./g, "").replace(/\,/g, "").replace(/đ/g, "").trim();
                var term = $(this).closest(".price-advice-box").find(".input-container-pathway").find("input").val();
                var paypermonth = Math.round(finalMoney / term);
                const lastTwoChars = levelValue2.slice(-2);
                const hoursLevel = parseInt(lastTwoChars, 10);
                var moneyFor2h = Math.round(finalMoney / (hoursLevel * (term / 3)) * 2);
                var moneyFor1h = Math.round(finalMoney / (hoursLevel * (term / 3)) * 1.5);
                var moneyFor45p = Math.round(finalMoney / (hoursLevel * (term / 3)) * 0.75);
                $(this).closest(".price-advice-box").find(".input-container-pricepermonth").find("input").val(paypermonth.toLocaleString() + 'đ');
                $(this).closest(".price-advice-box").find(".input-container-priceper2h").find("input").val(moneyFor2h.toLocaleString() + 'đ');
                $(this).closest(".price-advice-box").find(".input-container-priceper1-5h").find("input").val(moneyFor1h.toLocaleString() + 'đ');
                $(this).closest(".price-advice-box").find(".input-container-priceper45p").find("input").val(moneyFor45p.toLocaleString() + 'đ');
            }

            const pathway = $(this).val();
            const cth = $(this).closest(".price-advice-box").find(".input-container-cth").find("select").val();
            console.log("Pathway:", pathway);
            console.log("CTH:", cth);
            var items = [];
            items.push("<option value>Chọn ưu đãi</option>");
            var thisElement = $(this);
            //if (pathway !== "" && cth !== "") {
            $.getJSON("/Home/GetDiscount", { pathway: pathway, cth: cth }, function (data) {
                $.each(data, function (key, val) {
                    items.push("<option value='" + val.Id + "'>" + val.Username + "</option>");
                });
                thisElement.closest(".price-advice-box").find(".input-container-qdsale").find("select").html(items.join(""));
            });
            //}
            //else {
            //    $("[data-item=district]").html(items.join(""));
            //}
        });

        //Khi thay đổi cấp độ học
        $(".input-container-level select").on("change", function () {
            $(this).closest(".price-advice-box").find(".input-container-qdsale").find("select").prop("selectedIndex", 0);
            $(this).closest(".price-advice-box").find(".input-container-moneyprice").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-giftprice").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-finalprice").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-paymethod").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-prepay").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-postpaid").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-pricepermonth").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-term").find("select").prop("selectedIndex", 0);
            const levelValue = $(this).val(); // Cấp độ học
            const pathwayMonths = $(this).closest(".input-container-level").siblings(".input-container-pathway").find("input").val(); // Lộ trình

            const unitPrice = calculateUnitPrice(levelValue); // Tính đơn giá
            const totalPrice = calculateTotalPrice(unitPrice, pathwayMonths); // Tính thành tiền
            // Hiển thị đơn giá
            $(this).closest(".input-container-level").siblings(".input-container-unitprice").find("input").val(unitPrice ? `${unitPrice.toLocaleString()}đ` : "");
            //Hiển thị thành tiền
            $(this).closest(".input-container-level").siblings(".input-container-totalprice").find("input").val(totalPrice ? `${totalPrice.toLocaleString()}đ` : "");
            $(this).closest(".input-container-level").siblings(".input-container-finalprice").find("input").val(totalPrice ? `${totalPrice.toLocaleString()}đ` : "");
            if ($(this).closest(".price-advice-box").find(".input-container-finalprice").find("input").val() !== "") {
                var finalMoney = $(this).closest(".price-advice-box").find(".input-container-finalprice").find("input").val().replace(/\./g, "").replace(/\,/g, "").replace(/đ/g, "").trim();
                var term = $(this).closest(".price-advice-box").find(".input-container-pathway").find("input").val();

                var paypermonth = Math.round(finalMoney / term);
                $(this).closest(".price-advice-box").find(".input-container-pricepermonth").find("input").val(paypermonth.toLocaleString() + 'đ');
                const lastTwoChars = levelValue.slice(-2);
                const hoursLevel = parseInt(lastTwoChars, 10);
                var moneyFor2h = Math.round(finalMoney / (hoursLevel * (term / 3)) * 2);
                var moneyFor1h = Math.round(finalMoney / (hoursLevel * (term / 3)) * 1.5);
                var moneyFor45p = Math.round(finalMoney / (hoursLevel * (term / 3)) * 0.75);
                $(this).closest(".price-advice-box").find(".input-container-pricepermonth").find("input").val(paypermonth.toLocaleString() + 'đ');
                $(this).closest(".price-advice-box").find(".input-container-priceper2h").find("input").val(moneyFor2h.toLocaleString() + 'đ');
                $(this).closest(".price-advice-box").find(".input-container-priceper1-5h").find("input").val(moneyFor1h.toLocaleString() + 'đ');
                $(this).closest(".price-advice-box").find(".input-container-priceper45p").find("input").val(moneyFor45p.toLocaleString() + 'đ');
            }

        });
        $(".input-container-qdsale select").on("change", function () {
            var thisElement = $(this);
            var idDiscount = $(this).val();
            var totalMoney = $(this).closest(".price-advice-box").find(".input-container-totalprice").find("input").val();
            $.post("/Home/CalcMoney", { id: idDiscount, totalMoney: totalMoney }, function (data) {
                if (data.status) {
                    thisElement.closest(".price-advice-box").find(".input-container-moneyprice").find("input").val(data.moneyDiscount.toLocaleString() + 'đ');
                    thisElement.closest(".price-advice-box").find(".input-container-giftprice").find("input").val(data.gift);
                    thisElement.closest(".price-advice-box").find(".input-container-finalprice").find("input").val(data.finalMoney.toLocaleString() + 'đ');
                    if (thisElement.closest(".price-advice-box").find(".input-container-finalprice").find("input").val() !== "") {
                        var finalMoney = thisElement.closest(".price-advice-box").find(".input-container-finalprice").find("input").val().replace(/\./g, "").replace(/\,/g, "").replace(/đ/g, "").trim();
                        var term = thisElement.closest(".price-advice-box").find(".input-container-pathway").find("input").val();
                        const levelValue2 = thisElement.closest(".input-container-qdsale").siblings(".input-container-level").find("select").val(); // Cấp độ học

                        var paypermonth = Math.round(finalMoney / term);
                        thisElement.closest(".price-advice-box").find(".input-container-pricepermonth").find("input").val(paypermonth.toLocaleString() + 'đ');
                        const lastTwoChars = levelValue2.slice(-2);
                        const hoursLevel = parseInt(lastTwoChars, 10);
                        var moneyFor2h = Math.round(finalMoney / (hoursLevel * (term / 3)) * 2);
                        var moneyFor1h = Math.round(finalMoney / (hoursLevel * (term / 3)) * 1.5);
                        var moneyFor45p = Math.round(finalMoney / (hoursLevel * (term / 3)) * 0.75);
                        thisElement.closest(".price-advice-box").find(".input-container-priceper2h").find("input").val(moneyFor2h.toLocaleString() + 'đ');
                        thisElement.closest(".price-advice-box").find(".input-container-priceper1-5h").find("input").val(moneyFor1h.toLocaleString() + 'đ');
                        thisElement.closest(".price-advice-box").find(".input-container-priceper45p").find("input").val(moneyFor45p.toLocaleString() + 'đ');
                    }
                }
            });

            //if ($(this).closest(".price-advice-box").find(".input-container-finalprice").find("input").val() !== "") {
            //    var finalMoney = $(this).closest(".price-advice-box").find(".input-container-finalprice").find("input").val().replace(/\./g, "").replace(/\,/g, "").replace(/đ/g, "").trim();
            //    var term = $(this).closest(".price-advice-box").find(".input-container-pathway").find("select").val();

            //    var paypermonth = Math.round(finalMoney / term);
            //    $(this).closest(".price-advice-box").find(".input-container-pricepermonth").find("input").val(paypermonth.toLocaleString() + 'đ');
            //}
        });
        $(".input-container-paymethod select").on("change", function () {
            var paymethodVal = $(this).val();
            if (paymethodVal === "paynow") {
                $(this).closest(".price-advice-box").find(".input-container-paychanel").find("select").prop("selectedIndex", 0);
                $(this).closest(".price-advice-box").find(".input-container-prepay").find("input").val("");
                $(this).closest(".price-advice-box").find(".input-container-postpaid").find("input").val("");
                $(this).closest(".price-advice-box").find(".input-container-term").find("select").prop("selectedIndex", 0);
                var finalMoney = $(this).closest(".price-advice-box").find(".input-container-finalprice").find("input").val().replace(/\./g, "").replace(/\,/g, "").replace(/đ/g, "").trim();
                var term = $(this).closest(".price-advice-box").find(".input-container-pathway").find("input").val();
                var paypermonth = Math.round(finalMoney / term);
                $(this).closest(".price-advice-box").find(".input-container-pricepermonth").find("input").val(paypermonth.toLocaleString() + 'đ');
            }

        });
        $(".input-container-prepay input").on("change", function () {
            //var thisElement = $(this);
            var prepay = $(this).val().replace(/\./g, "").replace(/\,/g, "").replace(/đ/g, "").trim();
            var finalMoney = $(this).closest(".price-advice-box").find(".input-container-finalprice").find("input").val().replace(/\./g, "").replace(/\,/g, "").replace(/đ/g, "").trim();
            var remainpay = finalMoney - prepay;
            $(this).closest(".price-advice-box").find(".input-container-postpaid").find("input").val(remainpay.toLocaleString() + 'đ');
            $(this).closest(".price-advice-box").find(".input-container-term").find("select").prop("selectedIndex", 0);

        });
        $(".input-container-term select").on("change", function () {
            //var thisElement = $(this);
            var term = $(this).val();
            var remainpay = $(this).closest(".price-advice-box").find(".input-container-postpaid").find("input").val().replace(/\./g, "").replace(/\,/g, "").replace(/đ/g, "").trim();
            var paypermonth = Math.round(remainpay / term);

            $(this).closest(".price-advice-box").find(".input-container-pricepermonth").find("input").val(paypermonth.toLocaleString() + 'đ');

        });
        $(".price-advice-box-title > :first-child").on("click", function () {
            //var thisElement = $(this);
            var cth = $(this).closest(".price-advice-box").find(".input-container-cth").find("select option:selected").text();
            var level = $(this).closest(".price-advice-box").find(".input-container-level").find("select option:selected").text();
            var pathway = $(this).closest(".price-advice-box").find(".input-container-pathway").find("input").val();
            var paymethod = $(this).closest(".price-advice-box").find(".input-container-paymethod").find("select option:selected").text();
            var term = $(this).closest(".price-advice-box").find(".input-container-term").find("select option:selected").text();
            var qdsale = $(this).closest(".price-advice-box").find(".input-container-qdsale").find("select option:selected").text();
            var paychanel = $(this).closest(".price-advice-box").find(".input-container-paychanel").find("select option:selected").text();
            var startdate = $(this).closest(".price-advice-box").find(".start-date").val();
            var enddate = $(this).closest(".price-advice-box").find(".end-date").val();
            var unitprice = $(this).closest(".price-advice-box").find(".input-container-unitprice").find("input").val();
            var totalprice = $(this).closest(".price-advice-box").find(".input-container-totalprice").find("input").val();
            var moneyprice = $(this).closest(".price-advice-box").find(".input-container-moneyprice").find("input").val();
            var giftprice = $(this).closest(".price-advice-box").find(".input-container-giftprice").find("input").val();
            var finalprice = $(this).closest(".price-advice-box").find(".input-container-finalprice").find("input").val();
            var prepay = $(this).closest(".price-advice-box").find(".input-container-prepay").find("input").val();
            var postpaid = $(this).closest(".price-advice-box").find(".input-container-postpaid").find("input").val();
            var pricepermonth = $(this).closest(".price-advice-box").find(".input-container-pricepermonth").find("input").val();
            var note = $(this).closest(".price-advice-box").find("textarea").val();
            $(".price-advice").fadeOut(300, function () {
                $(".price-overview").fadeIn(300);
            });
            $(".input-cth").val($(".input-cth").val() + cth);
            $(".input-level").val($(".input-level").val() + level);
            $(".input-pathway").val($(".input-pathway").val() + pathway);
            $(".input-chanel").val($(".input-chanel").val() + paychanel);
            $(".input-paymethod").val($(".input-paymethod").val() + paymethod);
            $(".input-totalprice").val($(".input-totalprice").val() + finalprice);
            $(".input-moneyprice").val($(".input-moneyprice").val() + moneyprice);
            $(".input-giftprice").val($(".input-giftprice").val() + giftprice);
            $(".input-pricepermonth").val($(".input-pricepermonth").val() + pricepermonth);
            $(".input-note").val($(".input-note").val() + note);
            //$(".input-cth").val(cth);
        });
    });
    $(".price-overview .btn-back-square").on("click", function () {
        $(".price-overview").fadeOut(300, function () {
            $(".price-advice").fadeIn(300);
        });
        $(".input-cth").val("Chương trình học: ");
        $(".input-level").val("Cấp độ học: ");
        $(".input-pathway").val("Lộ trình học: ");
        $(".input-chanel").val("Kênh thanh toán: ");
        $(".input-paymethod").val("Hình thức thanh toán: ");
        $(".input-totalprice").val("Học phí: ");
        $(".input-moneyprice").val("Số tiền ưu đãi: ");
        $(".input-giftprice").val("Quà tặng: ");
        $(".input-pricepermonth").val("Học phí/ tháng: ");
        $(".input-note").val("Ghi chú: ");
    });

    $(".btn-zoomin").on("click", function () {
        $(".price-advice").css("width", "fit-content");
        $(".price-advice").css("padding", "0 40px");
        $(this).closest(".price-advice-box").css("width", "50vw");
        $(this).closest(".price-advice-box-container").find(".input-container > :first-child").css("width", "170px");
        $(this).closest(".price-advice-box-container").find(".input-container").css("font-size", "19px");

    });
    $(".btn-zoomout").on("click", function () {
        $(".price-advice").css("width", "unset");
        $(".price-advice").css("padding", "unset");
        $(this).closest(".price-advice-box").css("width", "31vw");
        $(this).closest(".price-advice-box-container").find(".input-container > :first-child").css("width", "130px");
        $(this).closest(".price-advice-box-container").find(".input-container").css("font-size", "14px");
    });
    $(".payment-open").on("click", function () {

        $(this).closest(".price-advice-box-container").find(".payment-container").toggleClass("active");
        if ($(this).html().includes("plus")) {
            $(this).html('<i class="fa-solid fa-minus"></i>')
        }
        else {
            $(this).html('<i class="fa-solid fa-plus"></i>')
        }
    });
    $(".price-open").on("click", function () {

        $(this).closest(".price-advice-box-container").find(".price-container").toggleClass("active");
        if ($(this).html().includes("plus")) {
            $(this).html('<i class="fa-solid fa-minus"></i>')
        }
        else {
            $(this).html('<i class="fa-solid fa-plus"></i>')
        }
    });
}
function ExportPdf() {
    //const margin = 10;

    //// Tạo đối tượng options với cấu hình lề và các tùy chọn khác
    //const options = {
    //    margin: margin,
    //    filename: 'output.pdf',
    //    html2canvas: { scale: 1 },
    //    jsPDF: { unit: 'mm', format: 'a4', orientation: 'portrait' }
    //};

    //// Lấy phần tử HTML để chuyển đổi thành PDF
    //const element = document.getElementById('source-html');

    //// Tạo và lưu tệp PDF với các tùy chọn đã thiết lập
    //html2pdf().from(element).set(options).save();

    const element = document.getElementById('source-html');
    const options = {
        margin: 0,
        filename: 'phieudangky.pdf',
        html2canvas: {
            scale: 3,
            useCORS: true // Hỗ trợ tải tệp CSS
        },
        jsPDF: { unit: 'mm', format: 'a5', orientation: 'landscape' }
    };

    // Đảm bảo CSS được áp dụng trước khi xuất
    html2pdf().from(element).set(options).save();
}
function face() {
    tick();
    $(".face-ielts").on("click", function () {
        if (isTick === false) {
            $(".face-index").fadeOut(300, function () {
                $(".face-ielts-slide").fadeIn(300);
                $('.face-ielts-slick').slick({
                    autoplay: true,
                    dots: false,
                    infinite: true,
                    speed: 1000,
                    slidesToShow: 5,
                    slidesToScroll: 5,
                    autoplaySpeed: 2000,
                    arrows: true,
                    nextArrow: '<button type="button" class="slick-next"></button>',
                    prevArrow: '<button type="button" class="slick-prev"></button>',
                    responsive: [
                        {
                            breakpoint: 1400,
                            settings: {
                                slidesToShow: 4,
                                slidesToScroll: 4,
                            }
                        }
                    ]
                });
            });
        }
    });
    $(".face-cambridge").on("click", function () {
        if (isTick === false) {
            $(".face-index").fadeOut(300, function () {
                $(".face-cambridge-slide").fadeIn(300);
                $('.face-cambridge-slick').slick({
                    autoplay: true,
                    dots: false,
                    infinite: true,
                    speed: 1000,
                    slidesToShow: 5,
                    slidesToScroll: 5,
                    autoplaySpeed: 2000,
                    arrows: true,
                    nextArrow: '<button type="button" class="slick-next"></button>',
                    prevArrow: '<button type="button" class="slick-prev"></button>',
                    responsive: [
                        {
                            breakpoint: 1400,
                            settings: {
                                slidesToShow: 4,
                                slidesToScroll: 4,
                            }
                        }
                    ]
                });
            });
        }
    });
    $(".face-oe").on("click", function () {
        if (isTick === false) {
            $(".face-index").fadeOut(300, function () {
                $(".face-oe-slide").fadeIn(300);
                $('.face-oe-slick').slick({
                    autoplay: true,
                    dots: false,
                    infinite: true,
                    speed: 1000,
                    slidesToShow: 5,
                    slidesToScroll: 5,
                    autoplaySpeed: 2000,
                    arrows: true,
                    nextArrow: '<button type="button" class="slick-next"></button>',
                    prevArrow: '<button type="button" class="slick-prev"></button>',
                    responsive: [
                        {
                            breakpoint: 1400,
                            settings: {
                                slidesToShow: 4,
                                slidesToScroll: 4,
                            }
                        }
                    ]
                });
            });
        }
    });
    $(".face .btn-back-square").on("click", function () {
        $(".face > .slide:not(.face-index)").fadeOut(300, function () {
            $(".face-index").fadeIn(300);
        });

    });
}
function text_title(text) {

    $(".btns-text p").text(text);
}
//function homeJs() {

//    $('.banner').slick({
//        autoplay: true,
//        dots: true,
//        infinite: true,
//        speed: 1000,
//        slidesToShow: 1,
//        slidesToScroll: 1,
//        autoplaySpeed: 2000,
//        arrows: false,
//        pauseOnHover: false,
//    });
//}
function pathway() {
    $(".star-index img").on("click", function () {
        var classStar = $(this).attr("class");
        $(".pathway-mid,.pathway-left,.logo-about,.star-index").fadeOut(300, function () {
            $(".slide." + classStar).fadeIn(300);
        });
    });
    $(".five-value-item").on("click", function () {
        $(".five-value-item").removeClass("active");
        $(this).addClass("active");
    });
    $(".menu-btn").on("click", function () {
        $(".btn-back-square-pathway").fadeOut(300);

    });
    $(".logo-anchor.pathway-home-btn").on("click", function () {
        $(".pathway-index").fadeOut(300, function () {
            $(".pathway-mid").fadeIn(300);
            $(".pathway-left").fadeIn(300);
            $(".logo-about").fadeIn(100);
        });
    });
    var pathway = "";
    $(document).ready(function () {
        let isDragging = false;
        let currentElement = null;

        function startDrag(e) {
            isDragging = true;
            currentElement = $(this);

            currentElement.css({
                position: "absolute",
                transform: "translate(-75%, -125%)",
                transition: "none",
            });

            let event = e.type === "mousedown" ? e : e.originalEvent.touches[0];
            currentElement.css({
                top: event.pageY + "px",
                left: event.pageX + "px",
            });

            e.preventDefault();
        }

        function doDrag(e) {
            if (isDragging && currentElement) {
                let event = e.type === "mousemove" ? e : e.originalEvent.touches[0];
                currentElement.css({
                    top: event.pageY + "px",
                    left: event.pageX + "px",
                });
            }
        }

        function endDrag() {
            if (isDragging && currentElement) {
                isDragging = false;

                // Lấy class thứ hai của phần tử đang kéo (ví dụ: 'toeic', 'ielts', ...)
                let secondaryClass = currentElement.attr("class").split(" ")[0];

                // Hiển thị phần tử tương ứng trong pathway-mid
                currentElement.css("display", "none"); // Ẩn phần tử sau khi nhả
                $(".pathway-mid ." + secondaryClass).css("display", "block");

                currentElement = null;
            }
        }

        // Gắn sự kiện cho chuột
        $(document).on("mousedown", ".move", startDrag);
        $(document).on("mousemove", doDrag);
        $(document).on("mouseup", endDrag);

        // Gắn sự kiện cho cảm ứng
        $(document).on("touchstart", ".move", startDrag);
        $(document).on("touchmove", doDrag);
        $(document).on("touchend", endDrag);
    });
    tick();
    $(".pathway-mid .tick").on("click", function () {
        if (isTick === false) {
            pathway = $(this).attr("class").split(" ")[0];
            $(".pathway-mid,.pathway-left").fadeOut(300, function () {
                $(".pathway-index").fadeIn(300);
                $(".pathway-index .slide").fadeOut(0);
                if (pathway === "discovery") {
                    text_title("CHƯƠNG TRÌNH HỌC / DISCOVERY ENGLISH 4-6 TUỔI")
                    //$(".btns-text p").text();
                }
                if (pathway === "challenge") {
                    text_title("CHƯƠNG TRÌNH HỌC / CHALLENGE ENGLISH 6-11 TUỔI")
                    //$(".btns-text p").text();
                }
                if (pathway === "focus") {
                    text_title("CHƯƠNG TRÌNH HỌC / FOCUS ENGLISH 11-16 TUỔI")
                    //$(".btns-text p").text();
                }
            });
            $(".logo-about").fadeOut(100);
            $(".star-index").fadeOut(100);
            //$(".logo-anchor").css("display", "flex");
        }
    });
    $(".btn-pathway").on("click", function () {
        if (pathway === "discovery") {
            $(".pathway-index > .slide:not(.pathway-discovery)").fadeOut(300, function () {
                $(".pathway-discovery").fadeIn(300);
            });
        }
        if (pathway === "challenge") {
            $(".pathway-index > .slide:not(.pathway-challenge)").fadeOut(300, function () {
                $(".pathway-challenge").fadeIn(300);
            });
        }
        if (pathway === "focus") {
            $(".pathway-index > .slide:not(.pathway-focus)").fadeOut(300, function () {
                $(".pathway-focus").fadeIn(300);
            });
        }
    });
    $(".btn-people").on("click", function () {
        if (pathway === "discovery") {
            $(".pathway-index > .slide:not(.overview-discovery)").fadeOut(300, function () {
                $(".overview-discovery").fadeIn(300);
            });
        }
        if (pathway === "challenge") {
            $(".pathway-index > .slide:not(.overview-challenge)").fadeOut(300, function () {
                $(".overview-challenge").fadeIn(300);
            });
        }
        if (pathway === "focus") {
            $(".pathway-index > .slide:not(.overview-focus)").fadeOut(300, function () {
                $(".overview-focus").fadeIn(300);
            });
        }
    });
    var slidenumber = 1;
    $(".overview-discovery .btn-back").on("click", function () {
        if (slidenumber === 2) {
            $(".overview2-discovery").fadeOut(300, function () {
                $(".overview1-discovery").fadeIn(300);
            });
            slidenumber = 1;
        }
        else if (slidenumber === 3) {
            $(".overview3-discovery").fadeOut(300, function () {
                $(".overview2-discovery").fadeIn(300);
                $(".overview-discovery-title").fadeIn(0);
            });
            slidenumber = 2;
        }

    });
    $(".overview-discovery .btn-next").on("click", function () {
        if (slidenumber === 1) {
            $(".overview1-discovery").fadeOut(300, function () {
                $(".overview2-discovery").fadeIn(300);
            });
            slidenumber = 2;
        }
        else if (slidenumber === 2) {
            $(".overview2-discovery").fadeOut(300, function () {
                $(".overview3-discovery").fadeIn(300);
                $(".overview-discovery-title").fadeOut(0);
            });
            slidenumber = 3;
        }
    });
    $(".eight-smart").on("click", function () {
        $(".pathway-index > .slide:not(.eight-smart-discovery)").fadeOut(300, function () {
            $(".eight-smart-discovery").fadeIn(300);
            $(".menu-btn").fadeOut(300);

        });
    });
    $(".eight-smart-discovery .btn-back-square").on("click", function () {
        $(".btn-people").trigger("click");
        $(".menu-btn").fadeIn(300);
    });
    $(".pathway-detail .btn-back-square").on("click", function () {
        $(".btn-pathway").trigger("click");
    });
    $(".menu-star a").on("click", function () {
        $(".menu-star a").removeClass("active");
        $(this).addClass("active");
        var textStarClass = $(this).attr("class").split(" ")[0];
        if (pathway === "discovery") {
            //$(".discovery-text-star img").fadeOut(300);
        }
        $(".title3").fadeOut(0);
        $(".pathway-text-star img").fadeOut(0);
        $(".pathway-text-star ." + textStarClass).fadeIn(0);
    });
    $(".pathways div").on("click", function () {
        var pwdetailClass = $(this).attr("class").split(" ")[0];
        $(".pathway-index > .slide:not(." + pwdetailClass + ")").fadeOut(300, function () {
            $(".pathway-index ." + pwdetailClass).fadeIn(300);

        });

        if (pathway === "discovery") {
            //$(".discovery-text-star img").fadeOut(300);
            $(".title3").fadeOut(0);
            $(".discovery-text-star img").fadeOut(0);

            $(".discovery-text-star ." + textStarClass).fadeIn(0);
        }
    });
    $(".pathway-detail-box").on("click", function () {
        $(".pathway-detail-box").removeClass("active");
        $(this).addClass("active");

    });

    $(".btn-book").on("click", function () {
        if (pathway === "discovery") {
            $(".pathway-index > .slide:not(.book-discovery)").fadeOut(300, function () {
                $(".book-discovery").fadeIn(300);
            });
        }
        if (pathway === "challenge") {
            $(".pathway-index > .slide:not(.challenge-book1-content)").fadeOut(300, function () {
                $(".challenge-book1-content").css("display", "flex");
            });
        }
        if (pathway === "focus") {
            $(".pathway-index > .slide:not(.book-focus)").fadeOut(300, function () {
                $(".book-focus").fadeIn(300);
            });
        }
    });
    $(".focus-book1").on("click", function () {
        $(".pathway-index > .slide:not(.book-focus-content1)").fadeOut(300, function () {
            $(".book-focus-content1").css("display", "flex");
        });
    });
    $(".focus-book2").on("click", function () {
        $(".pathway-index > .slide:not(.book-focus-content2-1)").fadeOut(300, function () {
            $(".book-focus-content2-1").fadeIn(300);
        });
    });
    $(".book-focus-content2-1 .btn-slide").on("click", function () {
        $(".pathway-index > .slide:not(.book-focus-content2-2)").fadeOut(300, function () {
            $(".book-focus-content2-2").fadeIn(300);
        });
    });
    $(".book-focus-content2-2 .btn-slide").on("click", function () {
        $(".pathway-index > .slide:not(.book-focus-content2-1)").fadeOut(300, function () {
            $(".book-focus-content2-1").fadeIn(300);
        });
    });
    $(".book-focus-content .btn-back-square").on("click", function () {
        $(".btn-book").trigger("click");
    });
    $(".discovery-book-book1 .img").on("click", function () {
        $(".pathway-index > .slide:not(.discovery-book-book1-content)").fadeOut(300, function () {
            //$(".discovery-book1-content").fadeIn(300);
            $(".discovery-book1-content").css("display", "flex");
            $(".menu-btn").fadeOut(300);
        });
    });
    $(".discovery-book-book2 .img").on("click", function () {
        $(".pathway-index > .slide:not(.discovery-book-book2-content)").fadeOut(300, function () {
            //$(".discovery-book2-content").fadeIn(300);
            $(".discovery-book2-content").css("display", "flex");
            $(".menu-btn").fadeOut(300);
        });
    });
    $(".starandfriend-text").on("click", function () {
        $(".pathway-index > .slide:not(.discovery-book-book3-content)").fadeOut(300, function () {
            //$(".discovery-book3-content").fadeIn(300);
            $(".discovery-book3-content").css("display", "flex");
            $(".menu-btn").fadeOut(300);
        });
    });
    $(".book1-challenge-text").on("click", function () {
        $(".pathway-index > .slide:not(.challenge-book2-content)").fadeOut(300, function () {
            $(".challenge-book2-content").css("display", "flex");
            //$(".discovery-book1-content").fadeIn(300);
            //$(".menu-btn").fadeOut(300);
        });
    });
    $(".book2-challenge-text").on("click", function () {
        $(".pathway-index > .slide:not(.challenge-book3-content)").fadeOut(300, function () {
            $(".challenge-book3-content").css("display", "flex");
            //$(".book-content-1").addClass("active");
            //$(".discovery-book1-content").fadeIn(300);
            //$(".menu-btn").fadeOut(300);
        });
    });
    $(".challenge-book3-content .book-content-1").on("click", function () {
        $(".challenge-book3-content .book-content").removeClass("active");
        $(".challenge-book3-content .book-content-2").addClass("active");
    });
    $(".challenge-book3-content .book-content-2").on("click", function () {
        $(".challenge-book3-content .book-content").removeClass("active");
        $(".challenge-book3-content .book-content-3").addClass("active");
    });
    $(".challenge-book3-content .book-content-3").on("click", function () {
        $(".challenge-book3-content .book-content").removeClass("active");
        $(".challenge-book3-content .book-content-4").addClass("active");
    });
    $(".challenge-book3-content .book-content-4").on("click", function () {
        $(".challenge-book3-content .book-content").removeClass("active");
        $(".challenge-book3-content .book-content-1").addClass("active");
    });
    $(".book-focus-content1 .book-content-1").on("click", function () {
        $(".book-focus-content1 .book-content").removeClass("active");
        $(".book-focus-content1 .book-content-2").addClass("active");
    });
    $(".book-focus-content1 .book-content-2").on("click", function () {
        $(".book-focus-content1 .book-content").removeClass("active");
        $(".book-focus-content1 .book-content-3").addClass("active");
    });
    $(".book-focus-content1 .book-content-3").on("click", function () {
        $(".book-focus-content1 .book-content").removeClass("active");
        $(".book-focus-content1 .book-content-4").addClass("active");
    });
    $(".book-focus-content1 .book-content-4").on("click", function () {
        $(".book-focus-content1 .book-content").removeClass("active");
        $(".book-focus-content1 .book-content-1").addClass("active");
    });

    $(".pathway-book-content .btn-back-square").on("click", function () {
        $(".btn-book").trigger("click");
        $(".menu-btn").fadeIn(300);
    });
    $(".btn-setting").on("click", function () {
        if (pathway === "discovery") {
            $(".pathway-index > .slide:not(.teaching-method-discovery)").fadeOut(300, function () {
                $(".teaching-method-discovery").fadeIn(300);
            });
        }
        if (pathway === "challenge") {
            $(".pathway-index > .slide:not(.teaching-method-challenge)").fadeOut(300, function () {
                $(".teaching-method-challenge").fadeIn(300);
                $(".teaching-method-challenge").css("display", "flex");
            });
        }
        if (pathway === "focus") {
            $(".pathway-index > .slide:not(.teaching-method-focus)").fadeOut(300, function () {
                $(".teaching-method-focus").fadeIn(300);
                $(".teaching-method-focus").css("display", "flex");
            });
        }
    });
    $(".teaching-method-name1").on("click", function () {
        $(".teaching-method-content img").removeClass("active");
        $(".teaching-method-content1").addClass("active");
    });
    $(".teaching-method-content1").on("click", function () {
        $(".teaching-method-content img").removeClass("active");
        $(".teaching-method-content2").addClass("active");
    });
    $(".teaching-method-content div").on("click", function () {
        $(".teaching-method-content img").removeClass("active");
        $(".teaching-method-content3").addClass("active");
    });
    $(".teaching-method-content2, .teaching-method-content3").on("click", function () {
        $(".teaching-method-content img").removeClass("active");
        $(".teaching-method-content1").addClass("active");
    });
    $(".teaching-method-name2").on("click", function () {
        pathway = "discovery";
        $(".btn-setting").trigger("click");
    });
    $(".teaching-method-focus1").on("click", function () {
        $(".pathway-index > .slide:not(.teaching-method-focus-content1)").fadeOut(300, function () {
            $(".teaching-method-focus-content1").fadeIn(300);
            $(".teaching-method-focus-content1").css("display", "flex");
        });
    });
    $(".teaching-method-focus2").on("click", function () {
        $(".pathway-index > .slide:not(.teaching-method-focus-content2)").fadeOut(300, function () {
            $(".teaching-method-focus-content2").fadeIn(300);
            $(".teaching-method-focus-content2").css("display", "flex");
        });
    });
    $(".teaching-method-focus-content .btn-back-square").on("click", function () {
        $(".btn-setting").trigger("click");
    });
    $(".teaching-method2 .teaching-name").on("click", function () {
        if (pathway === "discovery") {
            $(".teaching-after").removeClass("active");
            $(this).siblings(".teaching-after").addClass("active");
        }
    });
    $(".teaching-method2 .teaching-name1").on("click", function () {
        if (pathway === "discovery") {
            $(".teaching-content").removeClass("active");
            $(this).closest(".teaching-method2").find(".teaching-content1").addClass("active");
        }
    });
    $(".teaching-method2 .teaching-name2").on("click", function () {
        if (pathway === "discovery") {
            $(".teaching-content").removeClass("active");
            $(this).closest(".teaching-method2").find(".teaching-content2").addClass("active");
        }
    });
    $(".teaching-method2 .teaching-name3").on("click", function () {
        if (pathway === "discovery") {
            $(".teaching-content").removeClass("active");
            $(this).closest(".teaching-method2").find(".teaching-content3").addClass("active");
        }
    });
    $(".teaching-method2 .teaching-content1").on("click", function () {
        if (pathway === "discovery") {
            $(".teaching-content").removeClass("active");
            $(this).siblings(".teaching-content1-1").addClass("active");
        }
    });
    $(".teaching-method2 .teaching-content2").on("click", function () {
        if (pathway === "discovery") {
            $(".teaching-content").removeClass("active");
            $(this).siblings(".teaching-content2-1").addClass("active");
        }
    });
    $(".teaching-method2 .teaching-content3").on("click", function () {
        if (pathway === "discovery") {
            $(".teaching-content").removeClass("active");
            $(".teaching-content3-1").addClass("active");
        }
    });
    $(".teaching-method2 .teaching-content3-2-btn").on("click", function () {
        if (pathway === "discovery") {
            $(".teaching-content").removeClass("active");
            $(".teaching-content3-2").addClass("active");
        }
    });
    $(".btn-class").on("click", function () {
        if (pathway === "discovery") {
            $(".pathway-page").fadeOut(300, function () {
                $(".course-outline-discovery").fadeIn(300);
            });
        }
        if (pathway === "challenge") {
            $(".pathway-page").fadeOut(300, function () {
                $(".course-outline-challenge").fadeIn(300);
            });
        }
        if (pathway === "focus") {
            $(".pathway-page").fadeOut(300, function () {
                $(".course-outline-focus").fadeIn(300);
            });
        }
    });
    $(".course-outline .btn-back-square").on("click", function () {
        $(".course-outline").fadeOut(300, function () {
            $(".pathway-index > .slide").fadeOut(0);
            $(".pathway-page").fadeIn(300);
            $(".btn-back-square-pathway").fadeIn(300);
        });

    });
}

function tick() {
    var isMousedown = false;
    $(".tick").on("mousedown touchstart", function () {
        const thistick = $(this);
        timeout = setTimeout(function () {
            isTick = true;
            $(".tick").css('cursor', 'pointer');
            thistick.addClass("activetick");
            isMousedown = true;
        }, 700);
    });
    $(".tick").on("mouseup touchend", function () {
        clearTimeout(timeout);
    });


    $(".btn-tick-all").on("click", function () {
        $(".tick").filter(function () {
            return $(this).css("display") !== "none" && $(this).closest(":hidden").length === 0;
        }).toggleClass("activetick");

        // Kiểm tra xem có bất kỳ phần tử nào có lớp 'activetick'
        if ($(".activetick").length > 0) {
            isTick = true;
            $(".tick").css('cursor', 'pointer');
        }
        else {
            isTick = false;
            $(".tick").css('cursor', '');
        }
    });
    $(".tick").on("click", function () {

        if (isTick === true) {
            if (isMousedown === true) {
                $(this).addClass("activetick");
                isMousedown = false;
            }
            else {
                $(this).toggleClass("activetick");
            }
        }
        if ($(".activetick").length > 0) {
            isTick = true;
            $(".tick").css('cursor', 'pointer');
        }
        else {
            isTick = false;
            $(".tick").css('cursor', '');
        }
    });
    $(".btn-file").on("click", function () {
        $(".tick.activetick").addClass("hiddentick");
        $(".tick.activetick").fadeOut(300);

        $(".tick.activetick").removeClass("activetick");
        isTick = false;
        $(".tick").css('cursor', '');
    });
    $(".btn-backtick").on("click", function () {
        $(".tick.hiddentick").fadeIn(300);
        $(".tick.hiddentick").removeClass("hiddentick");
        $(".tick.activetick").removeClass("activetick");
        isTick = false;
        $(".tick").css('cursor', '');
    });
}
function about() {
    tick();
    $(".btn-map").on("click", function () {
        $(".about-page > .slide:not(.map-first)").fadeOut(300, function () {
            $(".map-first").fadeIn(300);
            $(".map-first").css("display", "flex");
        });
    });
    $(".map-first .btn-slide").on("click", function () {
        $(".about-page > .slide:not(.map-slide1)").fadeOut(300, function () {
            $(".map-slide1").fadeIn(300);
        });
    });
    $(".map-slide1 .btn-slide").on("click", function () {
        $(".about-page > .slide:not(.map-slide2)").fadeOut(300, function () {
            $(".map-slide2").fadeIn(300);
        });
    });
    $(".map-slide1 .btn-back-square,.map-slide2 .btn-back-square").on("click", function () {
        $(".btn-map").trigger("click");
    });
    $(".map-first .btn-map").on("click", function () {
        $(".offices-number").fadeIn(300);
        $(".offices-number").css('transform', 'translateX(0)');

    });
    $(".btn-north").on("click", function () {
        $(".map-content-title").fadeOut(0);
        $(".map-content-title").text('HỆ THỐNG TRUNG TÂM KHU VỰC MIỀN BẮC');
        $(".map-content-title").fadeIn(300);
        $(".locations-north").fadeIn(300);
        $(".locations-mid").fadeOut(300);
        $(".locations-south").fadeOut(300);
        $(".btn-mid img").css('width', '28px');
        $(".btn-south img").css('width', '28px');
        $(".btn-north img").css('width', '0');
        $(".offices-number").fadeOut(0);
        $(".offices-number").css('transform', 'translateX(0)');
        $(".offices-number").addClass("office-north-text");
        $(".offices-number").removeClass("office-south-text");
        $(".offices-number").removeClass("office-mid-text");
        $(".offices-number").html('Hơn <span>130</span> trung tâm trải dài các tỉnh/ thành phố');
        $(".offices-number").fadeIn(300);
        $(".content-slogan").fadeOut(300);
        //$(".office-list").fadeOut(0);
        $(".office-list").removeClass("active");
    });
    $(".btn-mid").on("click", function () {
        $(".map-content-title").fadeOut(0);
        $(".map-content-title").text('HỆ THỐNG TRUNG TÂM KHU VỰC MIỀN TRUNG');
        $(".map-content-title").fadeIn(300);
        $(".locations-mid").fadeIn(300);
        $(".locations-north").fadeOut(300);
        $(".locations-south").fadeOut(300);
        $(".btn-north img").css('width', '28px');
        $(".btn-south img").css('width', '28px');
        $(".btn-mid img").css('width', '0');
        $(".offices-number").fadeOut(0);
        $(".offices-number").css('transform', 'translateX(0)');
        $(".offices-number").addClass("office-mid-text");
        $(".offices-number").removeClass("office-south-text");
        $(".offices-number").removeClass("office-north-text");
        $(".offices-number").html('Hơn <span>40</span> trung tâm trải dài các tỉnh/ thành phố');
        $(".offices-number").fadeIn(300);
        $(".content-slogan").fadeOut(300);
        //$(".office-list").fadeOut(0);
        $(".office-list").removeClass("active");
    });
    $(".btn-south").on("click", function () {
        $(".map-content-title").fadeOut(0);
        $(".map-content-title").text('HỆ THỐNG TRUNG TÂM KHU VỰC MIỀN NAM');
        $(".map-content-title").fadeIn(300);
        $(".locations-south").fadeIn(300);
        $(".locations-north").fadeOut(300);
        $(".locations-mid").fadeOut(300);
        $(".btn-north img").css('width', '28px');
        $(".btn-mid img").css('width', '28px');
        $(".btn-south img").css('width', '0');
        $(".offices-number").fadeOut(0);
        $(".offices-number").css('transform', 'translateX(0)');
        $(".offices-number").addClass("office-south-text");
        $(".offices-number").removeClass("office-mid-text");
        $(".offices-number").removeClass("office-north-text");
        $(".offices-number").html('Gần <span>10</span> trung tâm tại thành phố Hồ Chí Minh');
        $(".offices-number").fadeIn(300);
        $(".content-slogan").fadeOut(300);
        //$(".office-list").fadeOut(0);
        $(".office-list").removeClass("active");
    });
    $(document).on("click", ".office-south-text", function () {
        $(".office-list").removeClass("active");
        $(this).siblings(".south").fadeIn(0);
        $(this).siblings(".south").addClass("active");
    });
    $(document).on("click", ".office-mid-text", function () {
        $(".office-list").removeClass("active");
        $(this).siblings(".mid").fadeIn(0);
        $(this).siblings(".mid").addClass("active");
    });
    $(document).on("click", ".office-north-text", function () {
        $(".office-list").removeClass("active");
        $(this).siblings(".north").fadeIn(0);
        $(this).siblings(".north").addClass("active");
    });
    $(".item").hide();
    $(".btn-mess").on("click", function () {
        $(".about-page > .slide:not(.slide-text)").fadeOut(300, function () {
            $(".about-page > .slide:not(.slide-text)").css("display", "none");
            $(".slide-text").fadeIn(300);
            $(".slide-text").css("display", "flex");
            $(".number span").countUp();
            $(".item1").show("slow");

            setTimeout(function () {
                $(".item2").show("slow");
            }, 200);

            setTimeout(function () {
                $(".item3").show("slow");
            }, 400);
        });
    });
    $(".btn-cup").on("click", function () {
        $(".about-page > .slide:not(.achie-index)").fadeOut(300, function () {
            $(".about-page > .slide:not(.achie-index)").css("display", "none");
            $(".btns-text").fadeOut(0);
            $(".logo-about").fadeOut(0);
            $(".menu-btn").fadeOut(0);
            $(".achie-index").fadeIn(300);
            $(".achie-index").css("display", "block");
        });
    });
    $(".btn-achie").on("click", function () {
        $(".about-page > .slide:not(.achie)").fadeOut(300, function () {
            $(".about-page > .slide:not(.achie)").css("display", "none");
            $(".btns-text").fadeOut(0);
            $(".logo-about").fadeOut(0);
            $(".menu-btn").fadeOut(0);
            $(".achie").fadeIn(300);
            $(".achie").css("display", "block");

            $('.achie-slick').slick({
                autoplay: false,
                dots: false,
                infinite: true,
                speed: 1000,
                slidesToShow: 5,
                slidesToScroll: 5,
                autoplaySpeed: 2000,
                arrows: true,
                nextArrow: '<button type="button" class="slick-next"></button>',
                prevArrow: '<button type="button" class="slick-prev"></button>',
                responsive: [
                    {
                        breakpoint: 1400,
                        settings: {
                            slidesToShow: 4,
                            slidesToScroll: 4,
                        }
                    }
                ]
            });
        });
    });

    $(".btn-user").on("click", function () {
        $(".about-page > .slide:not(.team)").fadeOut(300, function () {
            $(".about-page > .slide:not(.team)").css("display", "none");
            $(".btns-text").fadeOut(0);
            $(".logo-about").fadeOut(0);
            $(".team").fadeIn(300);
            $(".team").css("display", "block");
            $(".team").css("opacity", "1");
        });
    });
    $(".stars img").on("click", function () {
        $(".stars img").removeClass("active");
        $(this).addClass("active");
        var contentStarClass = $(this).attr("class").split(" ")[0];
        $(".star-content-container > *").removeClass("active");
        $(".star-content-container ." + contentStarClass).addClass("active");
    });
    $(".team-content-title").on("click", function () {
        $(".about-page > .slide:not(.team-slide)").fadeOut(300, function () {
            $(".team-slide").fadeIn(300);

            $('.team-slick').slick({
                autoplay: true,
                dots: false,
                infinite: true,
                speed: 1000,
                slidesToShow: 5,
                slidesToScroll: 5,
                autoplaySpeed: 2000,
                arrows: true,
                nextArrow: '<button type="button" class="slick-next"></button>',
                prevArrow: '<button type="button" class="slick-prev"></button>',
                responsive: [
                    {
                        breakpoint: 1400,
                        settings: {
                            slidesToShow: 4,
                            slidesToScroll: 4,
                        }
                    }
                ]
            });
        });

    });
    $(".team-slide .btn-back-square").on("click", function () {

        $(".btn-user").trigger("click");
    });
    $(".btn-hand").on("click", function () {
        $(".about-page > .slide:not(.hand-slide-1)").fadeOut(300, function () {
            $(".about-page > .slide:not(.hand-slide-1)").css("display", "none");
            $(".btns-text").fadeOut(0);
            $(".logo-about").fadeOut(0);
            $(".hand-slide-1").fadeIn(300);
            $('.hand-slide-1').css("display", "block");
        });
    });
    $(".hand-slide-1 .btn-slide").on("click", function () {
        $(".about-page > .slide:not(.hand-slide-2)").fadeOut(300, function () {
            $(".about-page > .slide:not(.hand-slide-2)").css("display", "none");
            $(".hand-slide-2").fadeIn(300);
            $(".hand-slide-2").css("display", "block");
        });
    });
    $(".hand-slide-2 .btn-slide").on("click", function () {
        $(".about-page > .slide:not(.hand-slide-1)").fadeOut(300, function () {
            $(".about-page > .slide:not(.hand-slide-1)").css("display", "none");
            $(".hand-slide-1").fadeIn(300);
            $(".hand-slide-1").css("display", "block");
        });
    });
    $(".hand-slide-1 .handslide-title").on("click", function () {
        $(".about-page > .slide:not(.promiss-1)").fadeOut(300, function () {
            $(".about-page > .slide:not(.promiss-1)").css("display", "none");
            $(".promiss-1").fadeIn(300);
            $(".promiss-1").css("display", "block");
            $(".menu-btn").fadeOut(300);
        });
    });

    $(".promiss-1 .btn-slide").on("click", function () {
        $(".about-page > .slide:not(.promiss-2)").fadeOut(300, function () {
            $(".about-page > .slide:not(.promiss-2)").css("display", "none");
            $(".promiss-2").fadeIn(300);
            $(".promiss-2").css("display", "block");
        });
    });
    $(".promiss-2 .btn-slide").on("click", function () {
        $(".about-page > .slide:not(.promiss-1)").fadeOut(300, function () {
            $(".about-page > .slide:not(.promiss-1)").css("display", "none");
            $(".promiss-1").fadeIn(300);
            $(".promiss-1").css("display", "block");
        });
    });
    $(".promiss-2 .btn-back-square,.promiss-1 .btn-back-square").on("click", function () {
        $(".about-page > .slide:not(.hand-slide-1)").fadeOut(300, function () {
            $(".about-page > .slide:not(.hand-slide-1)").css("display", "none");
            $(".hand-slide-1").fadeIn(300);
            $(".hand-slide-1").css("display", "block");
            $(".menu-btn").fadeIn(300);
        });
    });
    $(".btn-setting").on("click", function () {
        $(".about-page > .slide:not(.about-pathway)").fadeOut(300, function () {
            $(".about-page > .slide:not(.about-pathway)").css("display", "none");
            $(".btns-text").fadeOut(0);
            $(".logo-about").fadeOut(0);
            $(".about-pathway").fadeIn(300);
            $('.about-pathway').css("display", "block");
        });
    });
    $(".about-pathway-name").on("click", function () {
        $(".about-pathway-content").removeClass("active");
        $(this).siblings(".about-pathway-content").addClass("active");
    });
}
$("[data-item=city]").on("change", function (data) {
    const id = $(this).val();
    var items = [];
    items.push("<option value>Hãy chọn quận huyện</option>");

    if (id !== "") {
        $.getJSON("/Base/GetDistrict", { cityId: id }, function (data) {
            $.each(data, function (key, val) {
                items.push("<option value='" + val.Id + "'>" + val.Name + "</option>");
            });
            $("[data-item=district]").html(items.join(""));
        });
    } else {
        $("[data-item=district]").html(items.join(""));
    }
});
var today = new Date();
$(".datepicker").datepicker({
    dateFormat: 'dd/mm/yy',
    minDate: today,

}).attr('readonly', 'readonly');
$(".body-content table").addClass("table table-bordered").wrap("<div class='table-responsive' />");

