
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
$(".form-filter input").on("change", function (data) {
    let form = $(this).closest("form");
    if (form.valid()) { // Kiểm tra nếu form hợp lệ
        form.trigger('submit'); // Gọi sự kiện submit
    }
});

//$("[data-item=zone]").on("change", function (data) {
//    const id = $(this).val();
//    var items = [];
//    items.push("<option value>Chọn chi nhánh</option>");

//    if (id !== "") {
//        $.getJSON("/Home/GetOffice", { cityId: id }, function (data) {
//            $.each(data, function (key, val) {
//                items.push("<option value='" + val.Id + "'>" + val.Name + "</option>");
//            });
//            $("[data-item=office]").html(items.join(""));
//        });
//    }
//    else {
//        $("[data-item=office]").html(items.join(""));
//    }
//});
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
$(".btnedit-input").on("click", function () {
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
    var targetuser_month_BM = 0;
    var targetuser_month_real = 0;
    if (targetuser_month_HO_text !== "Chưa cập nhật") {
        var targetuser_month_HO = targetuser_month_HO_text.replace(/\,/g, "");
        targetuser_month_BM = $(this).closest("tr").find(".input-RevenueUser_Month_BMs").val().replace(/\,/g, "");
        if (targetuser_month_BM !== "" && targetuser_month_HO !== "") {
            var percent_user_month = Math.round(targetuser_month_BM / targetuser_month_HO * 100);
            $(this).siblings(".percent_user_month").text(percent_user_month);
            var debt = $(this).siblings(".debt-lastmonth").text().replace(/\,/g, "");
            var targetuser_month_real = Math.max((targetuser_month_BM - debt), 0);
            $(this).siblings(".real-target-month").text(targetuser_month_real.toLocaleString("en-US"));
        }
    }
    var currentWeek = $("input[name='CurrentWeek']").val();
    var totalWeekTarget = 0;
    //$(this).closest("tr").find(".input-RevenueUser_Week").each(function (index) {
    //    var weekTarget = $(this).val().trim().replace(/\,/g, "");
    //    if (weekTarget !== "" && targetuser_month_BM > 0) {
    //        var percent_user_week = Math.round(weekTarget / targetuser_month_BM * 100);
    //        $(this).closest("td").next(".week-percent").text(percent_user_week);
    //        totalWeekTarget += parseFloat(weekTarget) || 0;
    //    }
    //    if (/*targetuser_month_BM > 0 &&*/ totalWeekTarget !== targetuser_month_real) {
    //        if (index >= currentWeek) {
    //            $(this).siblings(".revenue-value-week_BM").text("");
    //            //$(this).siblings(".btnedit-input").css("display","none");
    //            $(this).closest("td").next("td").text("");
    //            $(this).val("");
    //            //$(this).css("display", "block");

    //        }
    //    }
    //});
    //var totalWeekTarget = 0;

    var elements = $(this).closest("tr").find(".input-RevenueUser_Week");
    elements.each(function (index) {
        var weekTarget = $(this).val().trim().replace(/\,/g, "");
        if (weekTarget !== "" && targetuser_month_BM > 0) {
            var percent_user_week = Math.round(weekTarget / targetuser_month_BM * 100);
            $(this).closest("td").next(".week-percent").text(percent_user_week);
            totalWeekTarget += parseFloat(weekTarget) || 0;
        }
    });
    //alert(totalWeekTarget);
    // Kiểm tra tổng số sau khi duyệt qua tất cả các phần tử
    if (totalWeekTarget !== targetuser_month_real) {
        elements.each(function (index) {
            if (index >= currentWeek - 1) {
                $(this).siblings(".revenue-value-week_BM").text("");
                $(this).closest("td").next("td").text("");
                $(this).val("");
            }
        });
    }
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
                var debt = thisElement.closest("tr").find(".debt-lastmonth").text().replace(/\,/g, "");
                var real_target_month = Math.max((targetBM - debt), 0);
                thisElement.closest("tr").find(".real-target-month").text(real_target_month.toLocaleString("en-US"));
                if (targetuser_month_HO_text !== "Chưa cập nhật") {
                    var targetuser_month_HO = targetuser_month_HO_text.replace(/\,/g, "");
                    var targetuser_month_BM = thisElement.val().replace(/\,/g, "");
                    var percent_user_month = Math.round(targetuser_month_BM / targetuser_month_HO * 100);
                    thisElement.closest("td").siblings(".percent_user_month").text(percent_user_month);
                    var currentWeek = $("input[name='CurrentWeek']").val();
                    thisElement.closest("tr").find(".revenue-value-week_BM").each(function (index) {
                        if (index >= currentWeek) {
                            $(this).siblings(".input-RevenueUser_Week").val("");
                            $(this).closest("td").next("td").text("");
                            $(this).text("");
                        }

                    });
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
        var target_Month_text = thisElement.closest("tr").find(".real-target-month").text().trim().replace(/\,/g, "");
        if (target_Month_text === "") {
            alert("Bạn chưa nhập chỉ tiêu dự kiến hoàn thành của tháng");
            thisElement.siblings(".btnedit-input").css("display", "block");
            thisElement.siblings(".revenue-value").css("display", "block");
            thisElement.css("display", "none");
            thisElement.val(thisElement.siblings(".revenue-value").text());
        }
        else {
            var totalWeekTarget = 0;
            var emptyCount = 0;

            var target_Month = parseFloat(target_Month_text);
            thisElement.closest("tr").find(".revenue-value-week_BM").each(function () {
                var weekTarget = $(this).text().trim().replace(/\,/g, "");
                var input_Week = $(this).siblings(".input-RevenueUser_Week").val().trim();
                if (input_Week === "")
                    emptyCount++;
                if ($(this).is(thisElement.siblings())) {
                    weekTarget = targetBM;
                }
                if (weekTarget !== "") {
                    totalWeekTarget += parseFloat(weekTarget) || 0;
                }
            });
            if (totalWeekTarget > target_Month) {

                alert("Tổng chỉ tiêu các tuần không được vượt quá chỉ tiêu của tháng");
                thisElement.siblings(".btnedit-input").css("display", "block");
                thisElement.siblings(".revenue-value").css("display", "block");
                thisElement.css("display", "none");
                thisElement.val(thisElement.siblings(".revenue-value").text());
            }
            else if (emptyCount < 1 && totalWeekTarget !== target_Month) {
                alert("Tổng chỉ tiêu các tuần phải bằng chỉ tiêu của tháng");
                thisElement.siblings(".btnedit-input").css("display", "block");
                thisElement.siblings(".revenue-value").css("display", "block");
                thisElement.css("display", "none");
                thisElement.val(thisElement.siblings(".revenue-value").text());
            }
            else {

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
        }
    }
});
$(function eventFunction() {
    $(".target-week").each(function () {
        var targetuser_week = parseFloat($(this).text().trim().replace(/\,/g, "")) || 0;
        var targetuser_week_BM = 0;
        var targetuser_week_real = 0;
        var currentDay = $("input[name='DayOfWeek']").val();
        var totalWeekTarget = 0;

        var elements = $(this).closest("tr").find(".input-RevenueUser_Day");
        elements.each(function (index) {
            var dayTarget = $(this).val().trim().replace(/\,/g, "");
            if (dayTarget !== "" && targetuser_week > 0) {
                totalWeekTarget += parseFloat(dayTarget) || 0;
            }
        });
        //alert(totalWeekTarget);
        // Kiểm tra tổng số sau khi duyệt qua tất cả các phần tử
        if (totalWeekTarget !== targetuser_week) {
            elements.each(function (index) {
                if (index >= currentDay - 1) {
                    $(this).siblings(".revenue-value").text("");
                    $(this).val("");
                }
            });
        }
    });
    $(".input-RevenueUser_Day").on("change", function () {
        var thisElement = $(this);
        if ($('.form-filter').valid()) {
            var userId = $(this).closest("tr").data("id");
            var targetBM = $(this).val().replace(/\,/g, "");
            //var targetBM_DT = $(this).closest("td").next(".day-target-number").find(".input-RevenueUser_Day_DT").val();
            var target_Week_text = thisElement.closest("tr").find(".target-week").text().trim().replace(/\,/g, "");
            if (target_Week_text === "") {
                alert("Bạn chưa nhập chỉ tiêu dự kiến hoàn thành của tuần");
                thisElement.siblings(".btnedit-input").css("display", "block");
                thisElement.siblings(".revenue-value").css("display", "block");
                thisElement.css("display", "none");
                thisElement.val(thisElement.siblings(".revenue-value").text());
            }
            else if (targetBM === "")
                $.toast({
                    text: 'Vui lòng nhập chỉ tiêu doanh số dự kiến',
                    icon: 'warning'
                })
            else {
                var totalDayTarget = 0;
                var emptyCount = 0;

                var target_Week = parseFloat(target_Week_text);
                thisElement.closest("tr").find(".revenue-value").each(function () {
                    var dayTarget = $(this).text().trim().replace(/\,/g, "");
                    var input_Day = $(this).siblings(".input-RevenueUser_Day").val().trim();
                    if (input_Day === "")
                        emptyCount++;
                    if ($(this).is(thisElement.siblings())) {
                        dayTarget = targetBM;
                    }
                    if (dayTarget !== "") {
                        totalDayTarget += parseFloat(dayTarget) || 0;
                    }
                });
                if (totalDayTarget > target_Week) {

                    alert("Tổng chỉ tiêu các ngày không được vượt quá chỉ tiêu của tuần");
                    thisElement.siblings(".btnedit-input").css("display", "block");
                    thisElement.siblings(".revenue-value").css("display", "block");
                    thisElement.css("display", "none");
                    thisElement.val(thisElement.siblings(".revenue-value").text());
                }
                else if (emptyCount < 1 && totalDayTarget !== target_Week) {
                    alert("Tổng chỉ tiêu các ngày phải bằng chỉ tiêu của tuần");
                    thisElement.siblings(".btnedit-input").css("display", "block");
                    thisElement.siblings(".revenue-value").css("display", "block");
                    thisElement.css("display", "none");
                    thisElement.val(thisElement.siblings(".revenue-value").text());
                }
                else {
                    year = $("select[name='Year']").val();
                    month = $("select[name='Month']").val();
                    week = $("select[name='Week']").val();
                    dayOfWeek = thisElement.siblings(".input-DayOfWeek").val();

                    $.post("/Event/AddOrUpdateRevenueDay", { year: year, month: month, targetBM: targetBM, userId: userId, week: week, dayOfWeek: dayOfWeek }, function (data) {
                        if (data.status) {
                            $.toast({
                                heading: 'Cập nhật thành công',
                                icon: 'success',
                                text: data.msg
                            })
                            thisElement.siblings(".btnedit-input").css("display", "block");
                            thisElement.siblings(".revenue-value").css("display", "block");
                            thisElement.css("display", "none");
                            thisElement.siblings(".revenue-value").text(thisElement.val());

                            //thisElement.closest("td").next(".day-target-number").find(".btnedit-input").css("display", "block");
                            //thisElement.closest("td").next(".day-target-number").find(".revenue-value").css("display", "block");
                            //thisElement.closest("td").next(".day-target-number").find(".input-RevenueUser_Day_DT").css("display", "none");
                            //thisElement.closest("td").next(".day-target-number").find(".revenue-value").text(targetBM_DT);

                        } else {
                            $.toast({
                                heading: 'Cập nhật thất bại',
                                icon: 'error',
                                text: data.msg
                            })
                            //location.reload();
                        }
                    });
                }

            }

        }
    });
});

//$(".input-RevenueUser_Day_DT").on("change", function () {
//    var thisElement = $(this);
//    if ($('.form-filter').valid()) {
//        var userId = $(this).closest("tr").data("id");
//        var targetBM_DT = $(this).val();
//        var targetBM_text = $(this).closest("td").prev(".day-target").find(".input-RevenueUser_Day").val();
//        var targetBM = targetBM_text.replace(/\,/g, "");
//        if (targetBM === "")
//            $.toast({
//                //heading: 'Cập nhật thất bại',
//                text: 'Vui lòng nhập chỉ tiêu doanh số dự kiến',
//                icon: 'warning'
//            })
//        else if (targetBM_DT === "")
//            $.toast({
//                //heading: 'Cập nhật thất bại',
//                text: 'Vui lòng nhập số lượng khách hàng chuyển đổi số dự kiến',
//                icon: 'warning'
//            })

//        else if (targetBM_DT.includes(",") || targetBM_DT.includes(".") || targetBM_DT.includes("-"))
//            $.toast({
//                //heading: 'Cập nhật thất bại',
//                text: 'Số lượng khách hàng chuyển đổi số dự kiến phải là số nguyên dương',
//                icon: 'warning'
//            })
//        else {
//            year = $("select[name='Year']").val();
//            month = $("select[name='Month']").val();
//            week = $("select[name='Week']").val();
//            dayOfWeek = thisElement.siblings(".input-DayOfWeek").val();
//            $.post("/Event/AddOrUpdateRevenueDay", { year: year, month: month, targetBM: targetBM, targetBM_DT: targetBM_DT, userId: userId, week: week, dayOfWeek: dayOfWeek }, function (data) {
//                if (data.status) {
//                    $.toast({
//                        heading: 'Cập nhật thành công',
//                        icon: 'success'
//                    })
//                    thisElement.siblings(".btnedit-input").css("display", "block");
//                    thisElement.siblings(".revenue-value").css("display", "block");
//                    thisElement.css("display", "none");
//                    thisElement.siblings(".revenue-value").text(thisElement.val());
//                    thisElement.closest("td").prev(".day-target").find(".btnedit-input").css("display", "block");
//                    thisElement.closest("td").prev(".day-target").find(".revenue-value").css("display", "block");
//                    thisElement.closest("td").prev(".day-target").find(".input-RevenueUser_Day").css("display", "none");
//                    thisElement.closest("td").prev(".day-target").find(".revenue-value").text(targetBM_text);

//                } else {
//                    $.toast({
//                        heading: 'Cập nhật thất bại',
//                        icon: 'error'
//                    })
//                    location.reload();
//                }
//            });
//        }

//    }
//});