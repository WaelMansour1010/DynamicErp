(function () {
    "use strict";

    var cards = document.querySelectorAll(".sync-card");
    for (var i = 0; i < cards.length; i += 1) {
        cards[i].setAttribute("data-loaded", "true");
    }

    var path = (window.location.pathname || "").toLowerCase();
    var links = document.querySelectorAll(".sync-nav a, #side-menu a");
    for (var j = 0; j < links.length; j += 1) {
        var href = (links[j].getAttribute("href") || "").toLowerCase();
        if (href !== "/" && path === href) {
            links[j].classList.add("active");
            var parent = links[j].closest("li");
            if (parent) {
                parent.classList.add("active");
            }
        }
    }

    // Chart.js can be added by deployment later. The CSS bars are the built-in fallback.
    if (!window.Chart) {
        return;
    }
}());
