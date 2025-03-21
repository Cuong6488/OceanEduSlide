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

