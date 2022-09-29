<p>private-page</p>

<?php

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

function formatErrors($errors)
{
	echo "<h1>SQL Error:</h1>";
	echo "Error information: <br/>";
	foreach ($errors as $error) {
		echo "SQLSTATE: " . $error['SQLSTATE'] . "<br/>";
		echo "Code: " . $error['code'] . "<br/>";
		echo "Message: " . $error['message'] . "<br/>";
	}
}

//
// Get Cookie 
//
$cookie_name = "_AspNetCore_Cookies";

// $_COOKIE[$cookie_name] = "for testing put a real cookie in here";

if (!isset($_COOKIE[$cookie_name])) {
	echo "Cookie named '" . $cookie_name . "' is not set! You should not be here. Redirect to /fire-map perhaps";
} else {

	$cookie_value = $_COOKIE[$cookie_name];

	//
	// Call database to see if this is a real cookie that we know about
	//

	// env variables are set in /etc/apache2/envvars
	// which are copied in the deploy script to the server 
	$serverName = getenv('DB_SERVERNAME');
	$database = getenv('DB_DATABASE');
	$username = getenv('DB_USERNAME');
	$password = getenv('DB_PASSWORD');

	// localhost testing
	// $serverName = "127.0.0.1";
	// $database = "OSR4Rights" ;
	// $username = "sa";
	// $password = "zpsecret";

	$connectionOptions = array(
		"database" => $database,
		"uid" => $username,
		"pwd" => $password,
		"TrustServerCertificate" => true
	);

	// Establishes db connection
	$conn = sqlsrv_connect($serverName, $connectionOptions);
	if ($conn === false) {
		die(formatErrors(sqlsrv_errors()));
	}

	$tsql = "
	SELECT c.LoginId, l.Email 
	FROM Cookie c 
	JOIN Login l on c.LoginId = l.LoginId
	WHERE CookieValue = ? 
	AND ExpiresUtc > GETUTCDATE()";

	$params = array($cookie_value);
	$stmt = sqlsrv_query($conn, $tsql, $params, array("Scrollable" => 'static'));

	if ($stmt === false) {
		die(formatErrors(sqlsrv_errors()));
	}

	$row_count = sqlsrv_num_rows($stmt);

	if ($row_count == 0) {
		echo "not authenticated!";
	} else {
		echo "authenticated - but not authorised yet ie could be tier1,2,or admin";

		while ($row = sqlsrv_fetch_array($stmt, SQLSRV_FETCH_ASSOC)) {
			$loginId = $row['LoginId'];
			$email = $row['Email'];
			echo "<br/><br/>loginId is " . $loginId;
			echo "<br/><br/>email is " . $email;
		}
	}

	sqlsrv_free_stmt($stmt);
	sqlsrv_close($conn);
}
?>