
$("#AlertBox").removeClass('hide');
$("#AlertBox").delay(5000).slideUp(500);

//$('#timepicker22').timepicker({
//    timeFormat: 'HH:mm',
//    interval: 30,
//    minTime: '00:00',
//    maxTime: '23:59',
//    startTime: '00:00',
//    dynamic: false,
//    dropdown: true,
//    scrollbar: true
//});
$(".form-filter select").on("change", function (data) {
    let form = $(this).closest("form");
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
$(".btn-edit-month").on("click", function () {
    $(this).css("display", "none");
    $(this).siblings(".revenue-value").css("display", "none");
    $(this).siblings("input").css("display", "block").focus().val(function (_, val) {
        return val; // Giữ nguyên giá trị hiện có
    }).each(function () {
        this.setSelectionRange(this.value.length, this.value.length); // Đưa con trỏ chuột về cuối
    });
    //$(".input-note").val($(".input-note").val() + note);
});
$(".btn-edit-week").on("click", function () {
    $(this).css("display", "none");
    $(this).siblings(".revenue-value").css("display", "none");
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
            var percent_user_month = Math.round(targetuser_month_BM / targetuser_month_HO * 100);
            $(this).siblings(".percent_user_month").text(percent_user_month);
        }
    }
    $(this).closest("tr").find(".input-RevenueUser_Week").each(function () {
        var weekTarget = $(this).val().trim().replace(/\,/g, "");
        if (weekTarget !== "") {
            var percent_user_week = Math.round(weekTarget / targetuser_month_BM * 100);
            $(this).closest("td").next(".week-percent").text(percent_user_week);
        }
    });
    $(this).closest("tr").find(".week-real").each(function () {
        var weekReal = $(this).text().trim().replace(/\,/g, "");
        if (weekReal !== "") {
            var percent_user_week = Math.round(weekReal / targetuser_month_BM * 100);
            $(this).closest("td").next("td").text(percent_user_week);
        }
    });

});
$(".input-RevenueUser_Month_BMs").on("change", function () {
    var thisElement = $(this);
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
                    var percent_user_month = Math.round(targetuser_month_BM / targetuser_month_HO * 100);
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
$(".input-RevenueUser_Week").on("change", function () {
    var thisElement = $(this);
    if ($('.form-filter form').valid()) {
        var userId = $(this).closest("tr").data("id");
        var targetBM = $(this).val().replace(/\,/g, "");
        year = $("select[name='Year']").val();
        month = $("select[name='Month']").val();
        weekNumber = thisElement.siblings(".input-weekNumber").val();
        $.post("/TuyenSinh/AddOrUpdateRevenueWeek", { year: year, month: month, targetBM: targetBM, userId: userId, weekNumber: weekNumber }, function (data) {
            if (data.status) {
                $.toast({
                    heading: 'Cập nhật thành công',
                    icon: 'success'
                })
                thisElement.siblings(".btnedit-input").css("display", "block");
                thisElement.siblings(".revenue-value").css("display", "block");
                thisElement.css("display", "none");
                thisElement.siblings(".revenue-value").text(thisElement.val());
                var targetuser_month_BM_text = thisElement.closest("tr").find(".input-RevenueUser_Month_BMs").val().trim();
                if (targetuser_month_BM_text !== "") {
                    var targetuser_month_BM = targetuser_month_BM_text.replace(/\,/g, "");
                    var targetuser_week = thisElement.val().replace(/\,/g, "");
                    var percent_user_week = Math.round(targetuser_week / targetuser_month_BM * 100);
                    thisElement.closest("td").next(".week-percent").text(percent_user_week);
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
