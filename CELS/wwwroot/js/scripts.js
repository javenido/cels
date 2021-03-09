/*
 * Source: https://codepen.io/diegoleme/pen/surIK
 */
function validatePassword() {
    var password = document.getElementById("Password"),
        confirm_password = document.getElementById("confirm_password");

    if (password.value != confirm_password.value) {
        confirm_password.setCustomValidity("Passwords don't match");
    } else {
        confirm_password.setCustomValidity('');
    }
}

function displayImageFileName(element) {
    var imageFileNameText = document.getElementById("imageFileName");
    imageFileNameText.innerHTML = element.value.split(/(\\|\/)/g).pop();
}

function displayResumeFileName(element) {
    var imageFileNameText = document.getElementById("resumeFileName");
    imageFileNameText.innerHTML = element.value.split(/(\\|\/)/g).pop();
}

function validateExtension(element, type) {
    var imageExtensions = ["jpeg", "jpg", "png"];
    var docExtensions = ["pdf"];
    var validExtensions = type == "img" ? imageExtensions : docExtensions;

    var fileExtension = element.value.split(".")[1];
    if (!validExtensions.includes(fileExtension)) {
        alert("The specified file cannot be uploaded. Only files with the following extensions are allowed: " + validExtensions.toString() + ".");
        element.value = "";
    }
}

function validateAvailability() {
    var from = document.getElementById("User_AvailabilityStart"),
        to = document.getElementById("User_AvailabilityEnd");

    if (from.value != "" && to.value != "") {
        var fromAsDate = new Date(), toAsDate = new Date();
        fromAsDate.setHours(from.value.split(':')[0], from.value.split(':')[1], 0);
        toAsDate.setHours(to.value.split(':')[0], to.value.split(':')[1], 0);

        if (fromAsDate > toAsDate) {
            to.setCustomValidity("Please specify a time after " + dateToString(fromAsDate));
        } else {
            to.setCustomValidity('');
        }
    }
}

function dateToString(date) {
    var hours = date.getHours();
    var minutes = date.getMinutes();
    var ampm = hours >= 12 ? 'PM' : 'AM'
    hours = hours % 12;
    hours = hours ? hours : 12;
    minutes = minutes < 10 ? '0' + minutes : minutes;
    return hours + ':' + minutes + ' ' + ampm;
}

function searchTeachers(buttonClicked) {
    if (event.key === 'Enter' || buttonClicked) {
        var url = "/Teacher/BrowseTeachers";

        var sortBy = document.getElementById("SortBy").value;
        url += "?sortby=" + sortBy;

        var order = document.getElementById("Order").checked;
        url += "&order=" + order;

        var searchKey = document.getElementById("SearchKey").value;
        if (searchKey.trim().length > 0) {
            url += "&search=" + searchKey;
        }
        window.location.href = url;
    }
}

function toggleOrder() {
    var checkBox = document.getElementById("Order");
    checkBox.checked = !checkBox.checked;
}