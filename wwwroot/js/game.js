﻿"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/gamehub").build();

connection.start().then(function () {
    console.log("Starting connection");
}).catch(function (err) {
    return console.error(err.toString());
});


var startButton = document.getElementById("start-button");

startButton.addEventListener("click", function (event) {

    console.log("Player ready to start")

    connection.invoke("UpdatePlayerReady").catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});


connection.on("AwaitPlayersReady", () => {
    var StartButton = document.getElementById("start-button");
   
    StartButton.removeAttribute('disabled')
})

connection.on("StartGame", () => {
    console.log("Both players ready");
    window.location.href = "https://localhost:44353/Game/Index";
    console.log("Redirected")
})




connection.on("PlayerReady", function (player) {
    console.log("Player Ready: " + player)
})

connection.on("OpponentReady", () => {
    document.getElementById("opponent-ready-message").removeAttribute("hidden");
})

