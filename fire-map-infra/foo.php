<?php
// if not authenticated redirect to /fire-map
// require("auth.php");
require("/header_alerts.php");

echo "<br />hello this is foo.php";
echo "<br />SESSION usercode is " . $_SESSION['usercode'];
echo "<br />SESSION email is " . $_SESSION['email'];

echo "<br />userid is " . $userid;
