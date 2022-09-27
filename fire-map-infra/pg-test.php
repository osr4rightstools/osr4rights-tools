PGTest
<?php

// show php errors otherwise will silently fail with no warnings on screen
// https://stackoverflow.com/a/21429652/26086
ini_set('display_errors', '1');
ini_set('display_startup_errors', '1');
error_reporting(E_ALL);

// env variables are set in /etc/apache2/envvars
$username = getenv('DBFIRE_USERNAME');
$password = getenv('DBFIRE_PASSWORD');

// $username = 'postgres';
// $password = 'verysecret123!!';

echo "username " . $username;
// echo "password " . $password;

try {
	$pdo = new PDO('pgsql:host=127.0.0.1;dbname=nasafiremap', $username, $password);

	// $sql = "select * from users;";
	$sql = "select version();";
	// $stmt = $pdo->prepare($sql);
	$stmt = $pdo->query($sql);
	$foo = $stmt->fetch();
	echo "foo is " . print_r($foo);

	$stmt = $pdo->query("select * from users");
	// $stmt is false if the query fails
	if (!$stmt) {
		die("Execute query error, because: " . print_r($pdo->errorInfo(), true));
	}

	$bar = $stmt->fetch();
	echo "bar is " . print_r($bar);


	echo "after bar query";
	echo "<br/>bar is " . print_r($bar);

	echo "done";
	// $stmt->execute([$s,$phash, $s]);	
	// $stmt->execute($s);

	// $c = $stmt->rowCount();

	// echo "c is " . $c;
} catch (PDOException $e) {

	echo "exception";
	die($e->getMessage());
}

echo "outside of try";
?>