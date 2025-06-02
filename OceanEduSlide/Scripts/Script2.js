
$("#AlertBox").removeClass('hide');
$("#AlertBox").delay(5000).slideUp(500);


$(".form-filter select").on("change", function (data) {

    let form = $('.form-filter form');
    if (form.valid()) { // Kiểm tra nếu form hợp lệ
        form.trigger('submit'); // Gọi sự kiện submit
    }
});

$(function () {
    $(".input-number").maskMoney({
        precision: 0,
        thousands: ','
    });
});
$(".btnedit-input").on("click", function () {
    $(this).css("display", "none");
    $(this).siblings(".revenue-value").css("display", "none");
    var cth = $(this).closest(".price-advice-box").find(".input-container-cth").find("select option:selected").text();
    $(this).siblings("input").css("display", "block").focus().val(function (_, val) {
        return val; // Giữ nguyên giá trị hiện có
    }).each(function () {
        this.setSelectionRange(this.value.length, this.value.length); // Đưa con trỏ chuột về cuối
    });
    //$(".input-note").val($(".input-note").val() + note);
});
$(".targetuser_month_HO").each(function () {
    var targetuser_month_HO_text = $(this).text().trim();
    if (targetuser_month_HO_text !== "Chưa cập nhật") {
        var targetuser_month_HO = targetuser_month_HO_text.replace(/\,/g, "");
        var targetuser_month_BM = $(this).closest("tr").find(".input-RevenueUser_Month_BMs").val().replace(/\,/g, "");
        if (targetuser_month_BM !== "" && targetuser_month_HO !== "") {
            var percent_user_month = Math.ceil(targetuser_month_BM / targetuser_month_HO * 100);
            $(this).siblings(".percent_user_month").text(percent_user_month);
        }

    }
});
$(".input-RevenueUser_Month_BMs").on("change", function () {
    var thisElement = $(this);
    let form = $('.form-filter form');
    if ($('.form-filter form').valid()) {
        var userId = $(this).closest("tr").data("id");
        var targetBM = $(this).val().replace(/\,/g, "");
        year = $("select[name='Year']").val();
        month = $("select[name='Month']").val();
        $.post("/TuyenSinh/AddOrUpdateRevenueMonth", { year: year, month: month, targetBM: targetBM, userId: userId }, function (data) {
            if (data.status) {
                $.toast({
                    heading: 'Cập nhật thành công',
                    icon: 'success'
                })
                thisElement.siblings(".btnedit-input").css("display", "block");
                thisElement.siblings(".revenue-value").css("display", "block");
                thisElement.css("display", "none");
                thisElement.siblings(".revenue-value").text(thisElement.val());
                var targetuser_month_HO_text = thisElement.closest("td").siblings(".targetuser_month_HO").text().trim();
                if (targetuser_month_HO_text !== "Chưa cập nhật") {
                    var targetuser_month_HO = targetuser_month_HO_text.replace(/\,/g, "");
                    var targetuser_month_BM = thisElement.val().replace(/\,/g, "");
                    var percent_user_month = Math.ceil(targetuser_month_BM / targetuser_month_HO * 100);
                    thisElement.closest("td").siblings(".percent_user_month").text(percent_user_month);
                }
            } else {
                $.toast({
                    heading: 'Cập nhật thất bại',
                    icon: 'error'
                })
                location.reload();
            }
        });
    }
});

