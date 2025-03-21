var leftMenuProfile = $.cookie("left_menu_profile");
if (leftMenuProfile == null) leftMenuProfile = "";
var arrMenu = leftMenuProfile.split('|');   
for (var i = 0; i < arrMenu.length; i++) {
    $("#left_menu_profile li[data-id='" + arrMenu[i] + "'] a.root").addClass("expand");
    $("#left_menu_profile li[data-id='" + arrMenu[i] + "'] div").show();
}
$(".left_menu_profile li .root").click(function () {
    $(this).parent().find(".sub").slideToggle(400);
    $(this).parent().find(".root").toggleClass("expand");
    var strTemp = $(this).parent().attr("data-id") + "|";
    leftMenuProfile = leftMenuProfile.replace(strTemp, "");
    if ($(this).hasClass("expand")) {
        leftMenuProfile = leftMenuProfile + strTemp;
    } 
    $.cookie("left_menu_profile", leftMenuProfile);
});
$("textarea.ckeditor").ckeditor();
CKEDITOR.timestamp = new Date();

$("[data-tour=parentProduct]").on("change", function () {
    var parentId = $(this).val();
    $.getJSON("/Vcms/GetCategory", { parentId: parentId }, function (data) {
        var items = [];
        items.push("<option value>Chọn danh mục</option>");
        $.each(data, function (key, val) {
            items.push("<option value='" + val.Id + "'>" + val.Name + "</option>");
        });
        $("[data-tour=categoryProduct]").html(items.join(""));
    });
});

$("input[type=file][data-upload-ajax=true]").on("change", function (e) {
    var files = e.target.files;
    var inputName = $(this).data("name");
    var maxWidth = $(this).data("m-width");
    var maxHeight = $(this).data("m-height");
    var folder = "projects";
    var getFolder = $(this).attr("data-upload-folder");
    if (getFolder != null && getFolder !== "undefined") {
        folder = getFolder;
    }
    if (files.length > 0) {
        var fileType = 1;
        if (window.FormData !== undefined) {
            var data = new FormData();
            for (var x = 0; x < files.length; x++) {
                var acceptFileTypes;
                if (inputName == "Documents") {
                    acceptFileTypes = /^image\/(gif|jpe?g|png)|text\/plain|application\/(vnd.openxmlformats-officedocument.wordprocessingml.document|pdf)$/i;
                    if (files[x]["type"].length && !acceptFileTypes.test(files[x]["type"])) {
                        alert("Only accept jpg, png, gif, jpeg, txt, pdf, docx");
                        return false;
                    }
                    fileType = 2;
                } else {
                    acceptFileTypes = /^image\/(gif|jpe?g|png)$/i;
                    if (files[x]["type"].length && !acceptFileTypes.test(files[x]["type"])) {
                        alert("Only accept jpg, png, gif, jpeg");
                        return false;
                    } 
                }
                 
                if (files[x]["size"] > 1024 * 4000) {
                    alert("File size is larger than 4MB. Please choose other file.");
                    return false;
                }
                data.append("file" + x, files[x]);
            }

            $.ajax({
                type: "POST",
                url: '/Vcms/UploadFile?fileType=' + fileType + '&folder=' + folder + '&wImg=' + maxWidth + "&hImg=" + maxHeight,
                //dataType: "json",
                contentType: false,
                processData: false,
                data: data,
                success: function (result) {
                    //console.log(result);
                    if (result.status == "0") {
                        if (result.ext == ".pdf") {
                            $("[data-result=" + inputName + "]").html('<img src="/content/images/pdf_file.png" width=100/> <a href="javascript:;" onclick=\"removeFile(\'' + result.fileName + '\', \'' + inputName + '\')\"><i class="fa fa-remove"></i></a>');
                        }
                        else if (result.ext == ".docx") {
                            $("[data-result=" + inputName + "]").html('<img src="/content/images/doc_file.png" width=100/> <a href="javascript:;" onclick=\"removeFile(\'' + result.fileName + '\', \'' + inputName + '\')\"><i class="fa fa-remove"></i></a>');
                        }
                        else if (result.ext == ".txt") {
                            $("[data-result=" + inputName + "]").html('<img src="/content/images/txt_file.png" width=100/> <a href="javascript:;" onclick=\"removeFile(\'' + result.fileName + '\', \'' + inputName + '\')\"><i class="fa fa-remove"></i></a>');
                        }
                        else {
                            $("[data-result=" + inputName + "]").html('<img src="/images/' + result.folder + '/' + result.fileName + '" width="300" style="vertical-align: middle;"/> <a href="javascript:;" onclick=\"removeFile(\'' + result.folder + '/' + result.fileName + '\', \'' + inputName + '\')\"><i class="fa fa-2x fa-remove"></i></a>');
                        }

                        $("input[name=" + inputName + "]").val(result.fileName);
                        $("input[name=" + inputName + "Upload]").val("");
                        $("[data-file-upload=" + inputName + "]").hide();

                    } else {
                        alert(result.message);
                    }
                },
                error: function (xhr, status, p3, p4) {
                    var err = "Error " + " " + status + " " + p3 + " " + p4;
                    if (xhr.responseText && xhr.responseText[0] == "{")
                        err = JSON.parse(xhr.responseText).Message;
                    console.log(err);
                }
            });
        } else {
            alert("This browser doesn't support HTML5 file uploads!");
        }
    }
});

function removeFile(file, divId) {
    if (confirm("Are you sure remove this File?")) {
        $.post("/Vcms/RemoveFile", { file }, function () {
            $("[data-file-upload=" + divId + "]").find("input[type=hidden]").val("");
            $("[data-file-upload=" + divId + "]").show();
            $("[data-result=" + divId + "]").html("");
        });
    }
}

$("[data-item=typepost]").on("change", function () {
    const type = $(this).find("option:selected").val();
    $.getJSON("/Vcms/GetCategoryByPost", { type: type }, function (data) {
        var items = [];
        items.push("<option value>Chọn danh mục</option>");
        $.each(data, function (key, val) {
            if (val.ParentId === null) {
                items.push("<option value='" + val.Id + "'>" + val.CategoryName + "</option>");

                $.each(data, function (key1, val1) {
                    if (val1.ParentId === val.Id) {
                        items.push("<option value='" + val1.Id + "'> -- " + val1.CategoryName + "</option>");
                    }
                });
            }
        });
        $("[data-item='article-category']").html(items.join(""));
    });
});

function showLoading() {
    const div = $("<div class='loading-overlay'/>").append("<i class='fal fa-spin fa-spinner'/>");
    $("body").append(div);
}

function hideLoading() {
    $(".loading-overlay").remove();
}

$("#AlertBox").removeClass('hide');
$("#AlertBox").delay(5000).slideUp(500);

function myFunction() {
    var x = document.getElementById("responsive");
    if (x.className === "") {
        x.className += " responsive";
    } else {
        x.className = "";
    }
}

function fbFunction() {
    var x = document.getElementById("form");
    if (x.className === "") {
        x.className += " show-form";
    } else {
        x.className = "";
    }
}

$("[data-item=root]").on("change", function (data) {
    const id = $(this).val();
    var items = [];
    items.push("<option value>Hãy chọn danh mục</option>");

    if (id !== "") {
        $.getJSON("/ProductVcms/GetProductCategory", { parentId: id }, function (data) {
            $.each(data,
                function (key, val) {
                    items.push("<option value='" + val.Id + "'>" + val.Name + "</option>");
                });
            $("[data-item=child]").html(items.join(""));
        });
    } else {
        $("[data-item=child]").html(items.join(""));
    }
});

$("[data-item=root-article]").on("change", function (data) {
    const id = $(this).val();
    var items = [];
    items.push("<option value>Hãy chọn danh mục</option>");

    if (id !== "") {
        $.getJSON("/Article/GetChildCategory", { parentId: id }, function (data) {
            $.each(data,
                function (key, val) {
                    items.push("<option value='" + val.Id + "'>" + val.Name + "</option>");
                });
            $("[data-item=child-article]").html(items.join(""));
        });
    } else {
        $("[data-item=child-article]").html(items.join(""));
    }
});

$("[data-action=update]").on("blur", function () {
    const proId = $(this).data("projects");
    const sort = $(this).val();
    if (sort !== "") {
        $.post("/ProjectAdmin/QuickUpdate", { proId: proId, sort: sort }, function (data) {
            if (data.status === 0) {
                alert(data.msg);
            }
        });
    }
})
$(".deleteAdmin").click(function () {
    var Username = $(this).attr("id");
    if (confirm("Bạn có chắc chắn xóa quản trị này không?")) {

        $.post("/Vcms/DeleteAdmin", { Username : Username}, function (data) {
            if (data.status) {
                $("tr[data-id= '" + Username + "']").fadeOut();
                $.toast({
                    text: " Xóa quản trị viên thành công",
                    position: "bottom-right",
                    icon: "success",
                })
            }
            else {
                var message = data.msg ?? "";
                $.toast({
                    text: "Xóa quản trị viên thất bại" + "\n" + message,
                    position: "bottom-right",
                    icon: "error",
                })
            }
        });
    }
});
$(".btn-group").click(function () {
    $(this).find(".dropdown-menu").slideToggle("fast");
});

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