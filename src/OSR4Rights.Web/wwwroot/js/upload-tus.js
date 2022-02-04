/* global tus */
/* eslint no-console: 0 */

var upload = null
var uploadIsRunning = false
var toggleBtn = document.querySelector('#toggle-btn')
var input = document.querySelector('input[type=file]')
var progress = document.querySelector('.progress')
//var progressBar = progress.querySelector('.bar')
var progressBar = progress.querySelector('.progress-bar')

var alertBox = document.querySelector('#support-alert')
//var uploadList = document.querySelector('#upload-list')

// will default to Infinity below
var chunkInput = document.querySelector('#chunksize')
// will default to 1 below
var parallelInput = document.querySelector('#paralleluploads')

var endpointInput = document.querySelector('#endpoint')

if (!tus.isSupported) {
    alertBox.classList.remove('hidden')
}

if (!toggleBtn) {
    throw new Error('Toggle button not found on this page. Aborting upload-demo. ')
}

toggleBtn.addEventListener('click', (e) => {
    e.preventDefault()

    if (upload) {
        if (uploadIsRunning) {
            upload.abort()
            toggleBtn.textContent = 'resume upload'
            uploadIsRunning = false
        } else {
            upload.start()
            toggleBtn.textContent = 'pause upload'
            uploadIsRunning = true
        }
    } else if (input.files.length > 0) {
        startUpload()
    } else {
        input.click()
    }
})

input.addEventListener('change', startUpload)

function startUpload() {
    var file = input.files[0]
    // Only continue if a file has actually been selected.
    // IE will trigger a change event even if we reset the input element
    // using reset() and we do not want to blow up later.
    if (!file) {
        return
    }

    var endpoint = endpointInput.value
    var chunkSize = parseInt(chunkInput.value, 10)
    if (isNaN(chunkSize)) {
        chunkSize = Infinity
    }

    var parallelUploads = parseInt(parallelInput.value, 10)
    if (isNaN(parallelUploads)) {
        parallelUploads = 1
    }

    toggleBtn.hidden = false;
    toggleBtn.textContent = 'pause upload'

    var options = {
        endpoint,
        //https://github.com/tus/tus-js-client/blob/master/docs/api.md#removefingerprintonsuccess
        removeFingerprintOnSuccess: true,
        chunkSize,
        retryDelays: [0, 1000, 3000, 5000],
        parallelUploads,
        metadata: {
            filename: file.name,
            filetype: file.type,
        },
        onError(error) {
            if (error.originalRequest) {
                if (window.confirm(`Failed because: ${error}\nDo you want to retry?`)) {
                    upload.start()
                    uploadIsRunning = true
                    return
                }
            } else {
                window.alert(`Failed because: ${error}`)
            }

            reset()
        },
        onProgress(bytesUploaded, bytesTotal) {
            var percentage = (bytesUploaded / bytesTotal * 100).toFixed(2)
            //progressBar.style.width = `${percentage}%`
            //progressBar.attr('aria-valuenow', percentage)
            $('.progress-bar').attr('aria-valuenow', percentage).css('width', percentage + '%');
            $("#foo").text(percentage + '%');
            //console.log(bytesUploaded, bytesTotal, `${percentage}%`)
        },
        onSuccess() {
            var anchor = document.createElement('a')
            anchor.textContent = `Download ${upload.file.name} (${upload.file.size} bytes)`
            anchor.href = upload.url
            anchor.className = 'btn btn-success'

            //uploadList.appendChild(anchor)

            var bar = upload.url.replace('/files/', '')
            var orig = upload.file.name;

            // need current url as could be on face-search, or hate-speech
            //var foo = window.location.href;
            // eg /face-search
            var foo = window.location.pathname;

            if (foo.endsWith('face-search')) {
                window.location.replace("/face-search-go?createdFileName=" + bar + "&origFileName=" + orig);
            }

            if (foo.endsWith('hate-speech')) {
                window.location.replace("/hate-speech-go?createdFileName=" + bar + "&origFileName=" + orig);
            }

            if (foo.endsWith('speech-parts')) {
                window.location.replace("/speech-parts-go?createdFileName=" + bar + "&origFileName=" + orig);
            }

            reset()

        },
    }

    upload = new tus.Upload(file, options)
    upload.findPreviousUploads().then((previousUploads) => {
        askToResumeUpload(previousUploads, upload)

        upload.start()
        uploadIsRunning = true
    })
}

function reset() {
    input.value = ''
    toggleBtn.textContent = 'start upload'
    upload = null
    uploadIsRunning = false
}

function askToResumeUpload(previousUploads, upload) {
    if (previousUploads.length === 0) return

    // only want last attempted upload
    //let text = 'You tried to upload this file previously at these times:\n\n'
    //previousUploads.forEach((previousUpload, index) => {
    //    text += `[${index}] ${previousUpload.creationTime}\n`
    //})
    //text += '\nEnter the corresponding number to resume an upload or press Cancel to start a new upload'

    let text = 'You tried to upload this file previously at this time:\n\n'
    //previousUploads.forEach((previousUpload, index) => {
    //    text += `[${index}] ${previousUpload.creationTime}\n`
    //})
    //text += '[0] ' + previousUploads[0].creationTime + '\n';
    text += previousUploads[0].creationTime + '\n';

    text += '\nOK to resume an upload or Cancel to start a new upload'

    // there used to be a box to type in (ie prompt)
    //const answer = prompt(text)

    var foo = confirm(text)

    // pressed the OK button so resume
    if (foo == true) {
        upload.resumeFromPreviousUpload(previousUploads[0])
    } else {
        // pressed cancel so re-upload
        return;
    }

    // cancel
    //if (answer === null) {
    //    return; //break out of the function early
    //}
    ////const index = parseInt(answer, 10)

    ////if (!isNaN(index) && previousUploads[index]) {
    ////    upload.resumeFromPreviousUpload(previousUploads[index])
    ////}

    //upload.resumeFromPreviousUpload(previousUploads[0])
}
