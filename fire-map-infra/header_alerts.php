<?php
	// session_start();
	// if(!isset($_SESSION['usercode']))
	// {
	// 	header("location: login_user.php");
	// 	die();
	// }
	// $userid=$_SESSION['usercode'];

	
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

// Get .NET Cookie 
$cookie_name = "_AspNetCore_Cookies";

$_COOKIE[$cookie_name] = "CfDJ8DvrQ7tcJMNCjjCn_rjeXcVcU5n8gOfXo8Yd6JVsDHWlEdNgbv5z1hcWneB4eXxBPU_fCi_qUaEQPC5pBkoHSI_ynkWOjnDujKa9TOXgTex1ZaYCnRkcjJXrL8AqxoeRu32MKOZlX0GHbkquhlklMWvHrDVb_qPe3GlWje8QUS5Y6x3lgeD0FrnGC5v_ADiwVQDnp0mjlVlaM4XUqxjYWnA-5J36E-sLOCTBKqVMAxycgwCT4eYUTCc0tskASlN5feXy_BGCtxtN5BhE8-FZZCyzX7Tj1EbRRnnHRn0EgM8r1V4ycgyTgCdLe6Kwi1y7oIIq4FRpdWdS4jk49grDOLuoNVBuIwABlpyHK7G1t1Wb7XFm1xTQ1S_kRTkFRAR3p1vh9_9zcYvam0AyMIkaw7c8IoWiLPrF1Vatt1s99ONWeeGMA5GW73Sya8yTv7QVQgWoaCciaKWubhPzLWMWqHM";

if (!isset($_COOKIE[$cookie_name])) {
	// No .NET cookie so redirect to login page on .NET side with a return url

	// header('Location: /login');
	header('Location: /');
	die();
} else {

	session_start();
	if(isset($_SESSION['usercode']))
	{
		// Have a PHP session already so don't need to check .NET cookie validity
		return;
	}

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

	$connectionOptions = array(
		"database" => $database,
		"uid" => $username,
		"pwd" => $password,
		"TrustServerCertificate" => true
	);

	// Establishes db connection
	$conn = sqlsrv_connect($serverName, $connectionOptions);
	if ($conn === false) {
		// unfriendly error here to user..
		// but can't access mssql db
		// todo make more friendly
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
		// database query problem
		// again we are dying
		// need to make more friendly
		die(formatErrors(sqlsrv_errors()));
	}

	$row_count = sqlsrv_num_rows($stmt);

	if ($row_count == 0) {
		// unusual - cookie problem in the database
		echo "unusual - cookie problem in the db - not authenticated!";
		echo "can't do a redirect as displaying this error!";
		die();
		
	} else {
		// success - assume only 1 row
		// echo "authenticated - but not authorised yet ie could be tier1,2,or admin";

		while ($row = sqlsrv_fetch_array($stmt, SQLSRV_FETCH_ASSOC)) {
			$loginId = $row['LoginId'];
			$email = $row['Email'];

			// echo "<br/>loginId is " . $loginId;
			// echo "<br/>email is " . $email;

			session_start();
			// eg loginId 5 is davemateer@gmail.com from mssql side
			// so use that on this PHP side
			$_SESSION['usercode']=$loginId;
			$_SESSION['email']=$email;
		}
	}

	sqlsrv_free_stmt($stmt);
	sqlsrv_close($conn);
}

// PB uses this in code as include is called on every private page
$userid=$_SESSION['usercode'];
?>

<link rel="stylesheet" href="style.css" />
<p style = "font-size: 75%"> Menu: [Logged in as: <?php echo $userid; echo $_SESSION['email'];?>]&nbsp&nbsp&nbsp&nbsp<a href="projectlist_you.php">[Your Projects]</a>&nbsp&nbsp&nbsp&nbsp<a href="addproject.php">[Add Project]</a>&nbsp&nbsp&nbsp&nbsp<a href="/">[Home]</a>&nbsp&nbsp&nbsp&nbsp<a href="/logout">[Log out]</a>
</p>
