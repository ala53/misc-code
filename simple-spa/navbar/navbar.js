$(document).ready(function () {
    processMenuHooks();
});


var navbarClosed = false;
function closeOrOpenNavbar() {
    if (navbarClosed) {
        navbarClosed = false;
        $("#backing").css({
            "min-width": "200px"
        });
        $("#backing").width('90%');
        $(".menu-home").fadeIn('slow');

        $('#navbarCloseWrapper').fadeOut(200, function () {
            $('#navbarCloseButton').text('< Close Menu');
        }).fadeIn(100)

    } else {
        navbarClosed = true;
        $("#backing").css({
            "min-width": "30px"
        });
        $("#backing").width(30);
        $(".menu-home").hide(200);

        $('#navbarCloseWrapper').fadeOut(200, function () {
            $('#navbarCloseButton').text('>');
        }).fadeIn(100)
    }
}



/*
The internal functions to register
*/

//items is an array of menuitem or you can pass a function - must not be anonymous
function registerMenu(menuName, menuItemArray) {
    /*
    <li class="menu-home">

        <a>Home</a>
        

        <ul class="menu-chapter">
            <li><a href="#"><span>Home</span></a></li>
            <li><a href="#"><span>About</span></a></li>
            <li><a href="#"><span>Info</span></a></li>
        </ul>
    </li>

    */
    var str = "<li class='menu-home'>"; //the opening tag
    str += "<a>" + menuName + "</a>"; //the title
    str += "<ul class='menu-chapter'>"; //and the start of submenu

    var idsList = [];

    for (index = 0; index < menuItemArray.length; ++index) {
        var item = menuItemArray[index];
        str += "<li><a href='" + item.target + "' " + (item.newTab ? "target='_blank'" : "") + "><span>" + item.name + "</span></a></li>";
    }


    str += "</ul></li>"; //the closing tag

    //and add to DOM
    $("#topnav").append(str);

    //and parse dynamic functions
    for (index = 0; index < idsList.length; ++index) {
        $("#" + idsList[index].id).click(idsList[index].func);
    }

    //and close
    navbarClosed = false;
    processMenuHooks();
    closeOrOpenNavbar();
}

function menuItem(name, target, newTab) {
    this.name = name;
    this.target = target;
    this.newTab = newTab || false;
}


/*

And the code for the menu

*/

function processMenuHooks() {
    //Code so that if a li has the class active and the submenu is visible, it slides up, and vice versa. Also shows that if li doesnt has class active it will switch to that li clicked and slideup an visible submenu and dropdow the one under the li clicked
    $("#topnav a").unbind();
    $("#topnav a").click(function () {
        var el = $(this).parent();
        if (el.hasClass('active') && $(this).next().is(':visible')) {
            var active = el.siblings('.active');
            $(this).next().slideUp();
        }
        else if (el.hasClass('active') && !$(this).next().is(':visible')) {
            var active = el.siblings('.active');
            $(this).next().slideDown();
        }
        else if (!el.hasClass('active')) {
            $(this).next().slideDown();
            var active = el.siblings('.active');
            active.children('ul:first').slideUp();
            active.removeClass('active');
            el.addClass('active');
        }
    });

    $('.menu-chapter a').unbind();

    $('.menu-chapter a').bind('click', function (event) {

        if (this.target == "_blank") {
            return;
        }
        event.preventDefault(); //Always override
        if (this.href.endsWith('/#') || this.pathname == window.location.hash.substring(2)) { return; } //its the address we're at, ignore
        window.history.pushState(this.href, window.location.pathname, window.location.pathname + "#!" + this.pathname + this.search);
        showLoadScreen();
        $.get(this.href, {}, function (response) {
            $('#contentPlaceholder').html(response);
            processLinks();
            hideLoadScreen();
        })
    })



}

window.onpopstate = function (e) {
    if (e.state) {
        if (e.state.endsWith('/#')) { return; } //its the address we're at, ignore
        showLoadScreen();
        $.get(e.state, {}, function (response) {
            $('#contentPlaceholder').html(response);
            processLinks();
            hideLoadScreen();
        })
    }
};


// Processes all the links in the page and hooks them
function processLinks() {
    $('#contentPlaceholder').find('a').unbind();

    $('#contentPlaceholder').on('click', 'a', function (event) {
        if (this.target != "_blank") {
            event.preventDefault(); //Always override
            if (this.href.endsWith('/#') || this.pathname == window.location.hash.substring(2)) { return; } //its the address we're at, ignore
            window.history.pushState(this.href, window.location.pathname, window.location.pathname + "#!" + this.pathname + this.search);
            showLoadScreen();
            $.get(this.href, {}, function (response) {
                $('#contentPlaceholder').html(response);
                processLinks();
                hideLoadScreen();
            });
        }
    });

    $('#contentPlaceholder').find('form').unbind();
    $('#contentPlaceholder').on('submit', 'form', function (ev) {
        showLoadScreen();
        console.log($(this).serialize());
        $.ajax({
            type: $(this).attr('method'),
            url: window.location.hash.substring(2) + window.location.search,
            data: $(this).serialize(),
            success: function (data) {
                console.log(data);
                $('#contentPlaceholder').html(data); // show response from the script.
                processLinks();
                hideLoadScreen();
            }
        });

        ev.preventDefault();
    });
}

if (typeof String.prototype.startsWith != 'function') {
    String.prototype.startsWith = function (str) {
        return this.substring(0, str.length) === str;
    }
};


if (typeof String.prototype.endsWith != 'function') {
    String.prototype.endsWith = function (str) {
        return this.substring(this.length - str.length, this.length) === str;
    }
};