var logo = "";

(function () {
    window.addEventListener("load", function () {
        setTimeout(function () {
            logo = document.getElementsByClassName('link');
            //For Changing The Link On The Logo Image                 
            logo[0].href = "https://restapi.tabfusionrms.com/swagger/index.html";
            // logo[0].target = "_blank";
            logo[0].children[0].alt = "rest api ";
            logo[0].children[0].src = "/tabfusion.png";
            var inter = setInterval(() => {
                if (logo[1] !== undefined) {
                    logo[1].innerHTML = "";
                    clearInterval(inter);
                }
            });
        });
    });
})();

