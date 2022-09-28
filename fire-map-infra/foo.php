<?php
// if not authenticated redirect to /fire-map
include("auth.php");

echo "<br />hello this is foo.php";
echo "<br />SESSION usercode is " . $_SESSION['usercode'];
echo "<br />userid is " . $userId;
