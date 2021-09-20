"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/osrHub").build();

// start and clicks the hidden crawl button
connection.start().then(function () {
    // connection established so turn on the crawl button
    var crawlButton = document.getElementById("crawlButton");
    crawlButton.disabled = false;
    // do an auto crawl when the page is hit for the first time
    crawlButton.click();
    //  disable the crawl button?
    crawlButton.disabled = true;
}).catch(function (err) {
    var li = document.createElement("li");
    li.textContent = err;
    // putting errors onto the list - TODO make nicer errors in red somewhere on UI
    document.getElementById("streamList").appendChild(li);
    return console.error(err.toString());
});

// called from the Hub
connection.on("NewFileMessage", function (item) {
    //var downloadButton = document.getElementById("downloadResultsButton");
    //var thing = "window.location.href = '/" + item.message + "'";
    //downloadButton.setAttribute("onClick", thing);
    var streamList = document.getElementById("streamList");
    var newItem = document.createElement("li");
    newItem.textContent = item.message;
    streamList.appendChild(newItem);
});

// hidden crawl button - which starts everything including the stream
document.getElementById("crawlButton").addEventListener("click", function (event) {
    //var urlToCrawl = document.getElementById("urlToCrawlHidden").value;
    var jobId = document.getElementById("jobIdHidden").value;
    //var subscription = connection.stream("Crawl", urlToCrawl)
    //var subscription = connection.stream("Counter", 17)
    var subscription = connection.stream("Counter", jobId)
        .subscribe({
            // getting a UIMessage object
            next: (item) => {
                var streamList = document.getElementById("streamList");
                var newItem = document.createElement("li");
                newItem.textContent = item;

                streamList.prepend(newItem);
                //streamList.appendChild(newItem);


                //newItem.setAttribute("id", "sli");

                //var html = "";
                //// urlIncludingBase === "" then just a status message
                //if (item.isStatusMessage === true) {
                //    //if (item.urlIncludingBase === "") {
                //    html = "" +
                //        item.message +
                //        "";
                //    newItem.innerHTML = html;
                //    streamList.prepend(newItem);
                //} else {
                //    // standard message with urls
                //    if (item.newLine === true) {
                //        html =
                //            "<a href='" +
                //            item.urlLinkedFrom +
                //            "' target='_blank'>" +
                //            item.urlLinkedFrom +
                //            "</a>" +
                //            " : " +
                //            "<a href='" +
                //            item.urlIncludingBase +
                //            "' target='_blank'>" +
                //            item.urlIncludingBase +
                //            "</a>";
                //        newItem.innerHTML = html;
                //        streamList.prepend(newItem);
                //    } else {
                //        // find the li with the uid and append this incoming text
                //        var lib = document.getElementById("sli");
                //        lib.append(" " + item.httpStatusCode + " " +
                //            item.elapsedMilliseconds + "ms");
                //    }
                //}

                // enable stop crawl button
                document.getElementById("stopCrawlButton").disabled = false;
            },
            complete: () => {
                var li = document.createElement("li");
                li.textContent = "Processing completed (js)";
                document.getElementById("streamList").prepend(li);
                // disable stop crawl button when complete
                document.getElementById("stopCrawlButton").disabled = true;
                // enable start button when complete
                document.getElementById("crawlButton").disabled = false;
                // enable download button 
                document.getElementById("downloadResultsButton").disabled = false;
            },
            error: (err) => {
                var li = document.createElement("li");

                var currentdate = new Date();

                li.textContent = currentdate.toTimeString() + " " + err + " (js)";
                // putting errors onto the list - TODO make nicer errors in red somewhere on UI
                //document.getElementById("streamList").appendChild(li);
                document.getElementById("streamList").prepend(li);
                // disable stop crawl button when error
                document.getElementById("stopCrawlButton").disabled = true;
                // enable start button when error
                document.getElementById("crawlButton").disabled = false;

            }
        });


    // wire up the cancel button
    // calling dispose causes cancellation on the server
    // https://docs.microsoft.com/en-us/aspnet/core/signalr/streaming?view=aspnetcore-3.0#server-to-client-streaming-2
    document.getElementById("stopCrawlButton").addEventListener("click",
        function (event) {
            subscription.dispose();
            // update the UI as no messages come back from server once cancelled
            var li = document.createElement("li");
            li.textContent = "Job cancelled";

            document.getElementById("streamList").prepend(li);

            // disable stop button when stop button has been pressed
            document.getElementById("stopCrawlButton").disabled = true;
        });

});
