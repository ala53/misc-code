function hideLoadScreen() {
    $('#loadingScreenBlur').fadeOut(150);
    $('#loading-icon').fadeOut(150);
    //Process links'

}

function showLoadScreen() {
    $('#loadingScreenBlur').fadeIn(150);
    $('#loading-icon').fadeIn(150);
    loadScreenHide404();
}

function loadScreen404() {
    document.getElementById("loading_error_msg").style.visibility = "";
    console.log("404")
}
function loadScreenHide404() {
    document.getElementById("loading_error_msg").style.visibility = "hidden";
}