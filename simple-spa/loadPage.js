$(document).ready(loadCoreData);

var loadPageDesiredPage = "/Content/home.html"
function loadCoreData() {
    if (window.location.hash) {
        // There is a desired page
        loadPageDesiredPage = window.location.hash.substring(2);
    }
    else {
        window.location.hash = "!" + loadPageDesiredPage;
    }
    //First check what to load
    $("#navbarPlaceholder").load("navbar/component_navbar.html", registerBaseMenus); //Load the navbar
    $("#loadingScreenPlaceholder").load("loadingscreen/component_loadingscreen.html", load_page);
}


/*

Register new menus

*/


function registerBaseMenus() {
    var home = [new menuItem("Home", "/content/home.html"),
                new menuItem("Newsletter", "/content/newsletter.html")];

    registerMenu("Home", home);

    var about = [new menuItem("Upcoming Events", "/Content/event.html"),
				 new menuItem("Essay", "/Content/essay.html"),
				 new menuItem("Oratorical", "/Content/oratorical.html")];

    registerMenu("Events", about);

    var about = [new menuItem("About", "/Content/about.html"),
                 new menuItem('Contact Us', '/Content/contact.html'),
                 new menuItem('Club History', '/Content/history.html'),
				 new menuItem('Youth Camp', '/Content/youthcamp.html')];

    registerMenu("About Us", about);

    var partners = [new menuItem('Optimist International', "http://optimist.org", true),
				   new menuItem('NW Optimist District', "http://www.pnwdistrictoptimist.com/", true),
				   new menuItem('Optimist Gresham', "http://www.greshamoptimistclub.com/", true),
				   new menuItem('Optimist Hillsboro', "http://www.hillsboro-optimists.org/", true),
				   new menuItem('Mentor Of Boys', "http://www.mentorsofboys.org/", true)];

    registerMenu("Partners", partners);

    var donate = [new menuItem('Donate - PayPal', "http://paypal.com/donate", true),
				   new menuItem('Donate - Google Wallet', "http://www.google.com/wallet/", true),
				   new menuItem('Donate - Bitcoin', "http://www.coinbase.com/", true)];

    registerMenu("Donate", donate);

    processMenuHooks();
}

/*

Loading function for pages

*/

function load_page() {
    showLoadScreen();
    $.get(loadPageDesiredPage, {}, function (response, err) {
        $('#contentPlaceholder').html(response);
        window.history.replaceState(loadPageDesiredPage, window.location.pathname, window.location.pathname + "#!" + loadPageDesiredPage);
        processLinks();
        hideLoadScreen();
    });
}