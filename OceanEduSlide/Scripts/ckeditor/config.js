/**
 * @license Copyright (c) 2003-2021, CKSource - Frederico Knabben. All rights reserved.
 * For licensing, see https://ckeditor.com/legal/ckeditor-oss-license
 */

CKEDITOR.editorConfig = function( config ) {
	// Define changes to default configuration here. For example: 
	// config.language = 'fr';
	// config.uiColor = '#AADC6E';

    config.removeDialogTabs = "image:advanced;link:advanced";
    config.autoGrow_maxHeight = 400;
    config.allowedContent = true;
    config.filebrowserBrowseUrl = "/Scripts/ckfinder/ckfinder.html";
    config.filebrowserImageBrowseUrl = "/Scripts/ckfinder/ckfinder.html?type=Images";
    config.filebrowserFlashBrowseUrl = "/Scripts/ckfinder/ckfinder.html?type=Flash";
    config.filebrowserUploadUrl = "/Scripts/ckfinder/core/connector/aspx/connector.aspx?command=QuickUpload&type=Files";
    config.filebrowserImageUploadUrl = "/Scripts/ckfinder/core/connector/aspx/connector.aspx?command=QuickUpload&type=Images";
    config.filebrowserFlashUploadUrl = "/Scripts/ckfinder/core/connector/aspx/connector.aspx?command=QuickUpload&type=Flash";

    config.extraPlugins = "btgrid,accordionList,collapsibleItem,video"; 
};
   