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
function pathway() {
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
    $(".overview-discovery .btn-back").on("click", function () {
        $(".overview2-discovery").fadeOut(300, function () {
            $(".overview1-discovery").fadeIn(300);
        });
    });
    $(".overview-discovery .btn-next").on("click", function () {
        $(".overview1-discovery").fadeOut(300, function () {
            $(".overview2-discovery").fadeIn(300);
        });
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
    });
    $(".discovery-book-book1").on("click", function () {
        $(".pathway-index > .slide:not(.discovery-book-book1-content)").fadeOut(300, function () {
            $(".discovery-book1-content").css("display", "flex");
            //$(".discovery-book1-content").fadeIn(300);
            $(".menu-btn").fadeOut(300);
        });
    });
    $(".discovery-book-book2").on("click", function () {
        $(".pathway-index > .slide:not(.discovery-book-book2-content)").fadeOut(300, function () {
            $(".discovery-book2-content").css("display", "flex");
            //$(".discovery-book1-content").fadeIn(300);
            $(".menu-btn").fadeOut(300);
        });
    });
    $(".starandfriend-text").on("click", function () {
        $(".pathway-index > .slide:not(.discovery-book-book3-content)").fadeOut(300, function () {
            $(".discovery-book3-content").css("display", "flex");
            //$(".discovery-book1-content").fadeIn(300);
            $(".menu-btn").fadeOut(300);
        });
    });
    $(".discovery-book-content .btn-back-square").on("click", function () {
        $(".btn-book").trigger("click");
        $(".menu-btn").fadeIn(300);
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

