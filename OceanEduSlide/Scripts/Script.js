AOS.init({
    offset: 200,
    once: true,
    duration: 500
});

function login() {
    $(".login-box a").on("click", function () {
        $(".first-login").fadeOut(300, function () { // Ẩn phần tử với hiệu ứng mờ dần
            $(".second-login").fadeIn(300); // Hiển thị phần tử với hiệu ứng mờ dần
        });
    });
}
function about() {
    var isTick = false;
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

