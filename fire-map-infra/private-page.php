<p>private-page!</p>
<br />

<?php
//
// Get Cookie from ASPNET
//
$cookie_name = "_AspNetCore_Cookies";

if (!isset($_COOKIE[$cookie_name])) {
	// no cookie, so not authenticated
	echo "Cookie named '" . $cookie_name . "' is not set! You should not be here. Redirect to /fire-map perhaps";
} else {
	// have a cookie with the correct name (which could be forged)

	$cookie_value = $_COOKIE[$cookie_name];
	echo "_AspNetCore_Cookies found for https://osr4rightstools.org and Value is: " . $cookie_value;

	//
	// Call database to see if this is a real cookie that we know about
	// read connection string from .ENV variable?
	//
	// also the email, loginId, RoleId (eg 1 - Tier 1, 2 - Tier 2, 9 - Admin)

	// read pwd from env variable set on server build - secrets never checked into source

	// env variables are set in /etc/apache2/envvars
	// which are copied in the deploy script to the server 
	// in /sercrets/fire-map-aoache-envvars.sh
	$serverName = getenv('DB_SERVERNAME');
	$database = getenv('DB_DATABASE');
	$username = getenv('DB_USERNAME');
	$password = getenv('DB_PASSWORD');

	$connectionOptions = array(
		"database" => $database,
		"uid" => $username,
		"pwd" => $password
	);

	function exception_handler($exception)
	{
		echo "<h1>Failure</h1>";
		echo "Exception: ", $exception->getMessage();
		echo "<h1>PHP Info for troubleshooting</h1>";
		phpinfo();
	}
	# default exception handler if not within a try/catch
	# eg when database is down or wrong connection string
	set_exception_handler('exception_handler');

	// Establishes db connection
	$conn = sqlsrv_connect($serverName, $connectionOptions);
	if ($conn === false) {
		die(formatErrors(sqlsrv_errors()));
	}

	//
	// Select Query
	//

	$tsql = "SELECT LoginId FROM Cookie WHERE CookieValue = ? AND ExpiresUtc > GETUTCDATE()";
	// $params = array("asdfjkahsdkjfhasdf");
	$params = array($cookie_value);

	$stmt = sqlsrv_query($conn, $tsql, $params);

	if ($stmt === false) {
		die(formatErrors(sqlsrv_errors()));
	}

	$row_count = sqlsrv_num_rows($stmt);

	echo "rows returned is: " . $row_count;
	if ($row_count == 0) {
		echo "not authenticated!";
	} else {
		echo "authenticated - but not authorised yet ie could be tier1,2,or admin";

		while ($row = sqlsrv_fetch_array($stmt, SQLSRV_FETCH_ASSOC)) {
			$loginId = $row['LoginId'];
			echo "loginId is " . $loginId;

			// echo $row['SQL_VERSION'] . PHP_EOL;
		}
	}


	// OLD
	// echo "<h1> Success Results : </h1>";

	// while ($row = sqlsrv_fetch_array($stmt, SQLSRV_FETCH_ASSOC)) {
	// 	echo $row['SQL_VERSION'] . PHP_EOL;
	// }

	sqlsrv_free_stmt($stmt);
	sqlsrv_close($conn);


	// where can this function be.. above the error handler?
	function formatErrors($errors)
	{
		// Display errors
		echo "<h1>SQL Error:</h1>";
		echo "Error information: <br/>";
		foreach ($errors as $error) {
			echo "SQLSTATE: " . $error['SQLSTATE'] . "<br/>";
			echo "Code: " . $error['code'] . "<br/>";
			echo "Message: " . $error['message'] . "<br/>";
		}
	}
}

?>