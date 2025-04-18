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

    var activeLinkId = sessionStorage.getItem('activeLinkId');
    if (activeLinkId) {
        $('#' + activeLinkId).addClass('active');
    }
    var currentPath = window.location.pathname;
    $('.footer-item').each(function () {
        if ($(this).attr('href') === currentPath) {
            $('.footer-item').removeClass('active');
            $(this).addClass('active');
            sessionStorage.setItem('activeLinkId', this.id);
        }

    });
    $('.footer-item').click(function (event) {
        event.preventDefault();
        $('.footer-item').removeClass('active');
        $(this).addClass('active');
        sessionStorage.setItem('activeLinkId', this.id);
        var url = $(this).attr('href');
        window.location.href = url;
    });
});
function price() {
    var today = new Date();
    $(".datepicker").datepicker({
        dateFormat: "dd/mm/yy",
        yearRange: "1900:2100",
        minDate: today,
    });

    $(".input-container-cth select").on("change", function (data) {
        $(this).closest(".price-advice-box").find(".input-container-qdsale").find("select").prop("selectedIndex", 0);
        $(this).closest(".price-advice-box").find(".input-container-moneyprice").find("input").val("");
        $(this).closest(".price-advice-box").find(".input-container-giftprice").find("input").val("");
        $(this).closest(".price-advice-box").find(".input-container-finalprice").find("input").val("");
        $(this).closest(".price-advice-box").find(".input-container-paymethod").find("input").val("");
        $(this).closest(".price-advice-box").find(".input-container-prepay").find("input").val("");
        $(this).closest(".price-advice-box").find(".input-container-postpaid").find("input").val("");
        $(this).closest(".price-advice-box").find(".input-container-pricepermonth").find("input").val("");
        $(this).closest(".price-advice-box").find(".input-container-term").find("select").prop("selectedIndex", 0);
        const id = $(this).val();
        var items = [];
        items.push("<option value>Chọn cấp độ học</option>");

        if (id !== "") {
            if (id === "pre-primary") {
                items.push('<option value="no-ielts">PRE-KINDY A</option>');
                items.push('<option value="no-ielts">PRE-KINDY B</option>');
                items.push('<option value="no-ielts">KINDY1A</option>');
                items.push('<option value="no-ielts">KINDY1B</option>');
                items.push('<option value="no-ielts">KINDY2A</option>');
                items.push('<option value="no-ielts">KINDY2B</option>');
                items.push('<option value="no-ielts">KINDY3A</option>');
                items.push('<option value="no-ielts">KINDY3B</option>');
            }
            else if (id === "primary") {
                items.push('<option value="no-ielts">PRE-KIDS A</option>');
                items.push('<option value="no-ielts">PRE-KIDS B</option>');
                items.push('<option value="no-ielts">PRE - KINDY B</option>');
                items.push('<option value="no-ielts">KIDS1A</option>');
                items.push('<option value="no-ielts">KIDS1B</option>');
                items.push('<option value="no-ielts">KIDS2A</option>');
                items.push('<option value="no-ielts">KIDS2B</option>');
                items.push('<option value="no-ielts">KIDS3A</option>');
                items.push('<option value="no-ielts">KIDS3B</option>');
                items.push('<option value="no-ielts">KIDS4A</option>');
                items.push('<option value="no-ielts">KIDS4B</option>');
                items.push('<option value="no-ielts">KIDS5A</option>');
                items.push('<option value="no-ielts">KIDS5B</option>');
            }
            else if (id === "high-school") {
                items.push('<option value="no-ielts">TEENS1A</option>');
                items.push('<option value="no-ielts">TEENS1B</option>');
                items.push('<option value="no-ielts">TEENS2A</option>');
                items.push('<option value="no-ielts">TEENS2B</option>');
                items.push('<option value="ielts">PRE-IELTS</option>');
                items.push('<option value="ielts">IELTS 4.0</option>');
                items.push('<option value="ielts">IELTS 4.5</option>');
                items.push('<option value="ielts">IELTS 5.0</option>');
                items.push('<option value="ielts">IELTS 5.5</option>');
                items.push('<option value="ielts">IELTS 6.0</option>');
            }
            else if (id === "ielts") {
                items.push('<option value="ielts">GN1A</option>');
                items.push('<option value="ielts">GN1B</option>');
                items.push('<option value="ielts">GN2A</option>');
                items.push('<option value="ielts">GN2B</option>');
                items.push('<option value="ielts">PRE-IELTS</option>');
                items.push('<option value="ielts">IELTS 4.0</option>');
                items.push('<option value="ielts">IELTS 4.5</option>');
                items.push('<option value="ielts">IELTS 5.0</option>');
                items.push('<option value="ielts">IELTS 5.5</option>');
                items.push('<option value="ielts">IELTS 6.0</option>');
                items.push('<option value="ielts">IELTS 6.5</option>');
                items.push('<option value="ielts">IELTS 7.0</option>');
                items.push('<option value="ielts">IELTS 7.5</option>');
                items.push('<option value="ielts">IELTS 8.0</option>');
                items.push('<option value="ielts">IELTS 8.5</option>');
            }
            else if (id === "toeic") {
                items.push('<option value="no-ielts">GN1A</option>');
                items.push('<option value="no-ielts">GN1B</option>');
                items.push('<option value="no-ielts">GN2A</option>');
                items.push('<option value="no-ielts">GN2B</option>');
                items.push('<option value="no-ielts">TOEIC 400</option>');
                items.push('<option value="no-ielts">TOEIC 450</option>');
                items.push('<option value="no-ielts">TOEIC 500</option>');
                items.push('<option value="no-ielts">TOEIC 550</option>');
                items.push('<option value="no-ielts">TOEIC 600</option>');
                items.push('<option value="no-ielts">TOEIC 650</option>');
                items.push('<option value="no-ielts">TOEIC 700</option>');
                items.push('<option value="no-ielts">TOEIC 750</option>');
                items.push('<option value="no-ielts">TOEIC 800</option>');
                items.push('<option value="no-ielts">TOEIC 850</option>');
                items.push('<option value="no-ielts">TOEIC 900</option>');
                items.push('<option value="no-ielts">TOEIC 950</option>');
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
            if (levelValue === "ielts") {
                return 3989000; // Đơn giá cho cấp độ "ielts"
            } else if (levelValue === "no-ielts") {
                return 2989000; // Đơn giá cho cấp độ "no-ielts"
            } else {
                return ""; // Đơn giá rỗng nếu không có giá trị
            }
        }

        // Hàm tính toán thành tiền
        function calculateTotalPrice(unitPrice, pathwayMonths) {
            if (unitPrice && pathwayMonths) {
                return unitPrice * pathwayMonths; // Thành tiền = đơn giá * số tháng
            } else {
                return ""; // Thành tiền rỗng nếu thiếu giá trị
            }
        }

        // Khi thay đổi thời gian bắt đầu
        $(".start-date").on("change", function () {
            const startDateValue = $(this).val();
            const pathwayMonths = $(".input-container-pathway select").val();

            if (startDateValue && pathwayMonths) {
                const startDate = parseDate(startDateValue);
                startDate.setMonth(startDate.getMonth() + parseInt(pathwayMonths));
                $(".end-date").val(formatDate(startDate)); // Cập nhật thời gian kết thúc
            } else {
                $(".end-date").val(""); // Đặt thời gian kết thúc về chuỗi rỗng
            }

        });

        //Khi thay đổi lộ trình
        $(".input-container-pathway select").on("change", function () {
            $(this).closest(".price-advice-box").find(".input-container-qdsale").find("select").prop("selectedIndex", 0);
            $(this).closest(".price-advice-box").find(".input-container-moneyprice").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-giftprice").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-finalprice").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-paymethod").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-prepay").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-postpaid").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-pricepermonth").find("input").val("");
            $(this).closest(".price-advice-box").find(".input-container-term").find("select").prop("selectedIndex", 0);
            const pathwayMonths = $(this).val();
            const startDateValue = $(".start-date").val();

            if (startDateValue && pathwayMonths) {
                const startDate = parseDate(startDateValue);
                startDate.setMonth(startDate.getMonth() + parseInt(pathwayMonths));
                $(".end-date").val(formatDate(startDate)); // Cập nhật thời gian kết thúc
            } else {
                $(".end-date").val(""); // Đặt thời gian kết thúc về chuỗi rỗng
            }

            const levelValue2 = $(this).closest(".input-container-pathway").siblings(".input-container-level").find("select").val(); // Cấp độ học

            const pathwayMonths2 = $(this).val(); // Lộ trình

            const unitPrice2 = calculateUnitPrice(levelValue2); // Tính đơn giá
            const totalPrice2 = calculateTotalPrice(unitPrice2, pathwayMonths2); // Tính thành tiền
            //Hiển thị thành tiền
            $(this).closest(".input-container-pathway").siblings(".input-container-totalprice").find("input").val(totalPrice2 ? `${totalPrice2.toLocaleString()}đ` : "");
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
            const pathwayMonths = $(this).closest(".input-container-level").siblings(".input-container-pathway").find("select").val(); // Lộ trình

            const unitPrice = calculateUnitPrice(levelValue); // Tính đơn giá
            const totalPrice = calculateTotalPrice(unitPrice, pathwayMonths); // Tính thành tiền
            // Hiển thị đơn giá
            $(this).closest(".input-container-level").siblings(".input-container-unitprice").find("input").val(unitPrice ? `${unitPrice.toLocaleString()}đ` : "");
            //Hiển thị thành tiền
            $(this).closest(".input-container-level").siblings(".input-container-totalprice").find("input").val(totalPrice ? `${totalPrice.toLocaleString()}đ` : "");
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

                }
            });
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
        $(".price-advice-box-title").on("click", function () {
            //var thisElement = $(this);
            var cth = $(this).closest(".price-advice-box").find(".input-container-cth").find("select option:selected").text();
            var level = $(this).closest(".price-advice-box").find(".input-container-level").find("select option:selected").text();
            var pathway = $(this).closest(".price-advice-box").find(".input-container-pathway").find("select option:selected").text();
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
        $(".input-chanel").val("Kênh trả góp: ");
        $(".input-paymethod").val("Hình thức thanh toán: ");
        $(".input-totalprice").val("Học phí: ");
        $(".input-moneyprice").val("Số tiền ưu đãi: ");
        $(".input-giftprice").val("Quà tặng: ");
        $(".input-pricepermonth").val("Học phí/ tháng: ");
        $(".input-note").val("Ghi chú: ");
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
        jsPDF: { unit: 'mm', format: 'a4', orientation: 'landscape' }
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
function pathway() {
    $(".logo-anchor.pathway-home-btn").on("click", function () {
        $(".pathway-index").fadeOut(300, function () {
            $(".pathway-mid").fadeIn(300);
            $(".logo-about").fadeIn(100);
            $(".logo-anchor.pathway-home-btn").fadeIn(300);
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
                if (pathway === "discovery") {
                    $(".btns-text p").text("CHƯƠNG TRÌNH HỌC / DISCOVERY ENGLISH 4-6 TUỔI");
                }
            });
            $(".logo-about").fadeOut(100);
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
            //$(".discovery-book1-content").fadeIn(300);
            //$(".menu-btn").fadeOut(300);
        });
    });
    $(".pathway-book-content .btn-back-square").on("click", function () {
        $(".btn-book").trigger("click");
        $(".menu-btn").fadeIn(300);
    });
    $(".btn-class").on("click", function () {
        if (pathway === "discovery") {
            //$(".pathway-index > .slide:not(.book-discovery)").fadeOut(300, function () {
            //    $(".book-discovery").fadeIn(300);
            //});
        }
        if (pathway === "challenge") {
            $(".pathway-index > .slide:not(.teaching-method-challenge)").fadeOut(300, function () {
                $(".teaching-method-challenge").fadeIn(300);
                $(".teaching-method-challenge").css("display", "flex");
            });
        }
    });
    $(".teaching-method-name").on("click", function () {
        if (pathway === "discovery") {
            //$(".pathway-index > .slide:not(.book-discovery)").fadeOut(300, function () {
            //    $(".book-discovery").fadeIn(300);
            //});
        }
        if (pathway === "challenge") {
            $(this).siblings(".teaching-method-content").addClass("active");

        }
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
        $(".tick.activetick").fadeOut(300);

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

