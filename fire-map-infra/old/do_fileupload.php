<?php
// note this is a copy from PB's repo as I have a slightly different directory 
// not using firemapweb
$userid=$_POST['userid'];
$projectid=$_POST['projectid'];


$target_dir = "uploads/";
$target_file = $target_dir.basename($_FILES["fileToUpload"]["name"]);
$target_file_save = $target_dir."user_".$userid."_".basename($_FILES["fileToUpload"]["name"]);

$uploadOk = 1;
$geoFileType = strtolower(pathinfo($target_file,PATHINFO_EXTENSION));

$username = getenv('DBFIRE_USERNAME');
$password = getenv('DBFIRE_PASSWORD');

// Check if file already exists
if (file_exists($target_file)) {
  echo "Sorry, file already exists.";
  $uploadOk = 0;
}

// Check file size
if ($_FILES["fileToUpload"]["size"] > 5000000) {
  echo "Sorry, your file is too large.";
  $uploadOk = 0;
  die;
}

// Allow certain file formats
if($geoFileType != "zip" && $geoFileType != "geojson" && $geoFileType != "gpkg") {
  echo "Sorry the only files which can be uploaded are: zip (for Shapefiles), geojson,  gpkg.";
  $uploadOk = 0;
  die;
}

// Check if $uploadOk is set to 0 by an error
if ($uploadOk == 0) {
  echo "Sorry, your file was not uploaded.";
  die;
// if everything is ok, try to upload file
} else {
  if (move_uploaded_file($_FILES["fileToUpload"]["tmp_name"], $target_file_save)) 
  {
        echo "The file ". basename( $_FILES["fileToUpload"]["name"]). " has been uploaded.";

      //unzip if needed
      if ($geoFileType=="zip") {
        echo ("<br>unzipping file<br>");

        $tmstmp = strtotime("now");
        $fn = $userid."_".$tmstmp;

        $cmd = "unzip  /var/www/html/$target_file_save -d /var/www/html/uploads/$fn  2>&1";
        //echo ($cmd);
        echo shell_exec($cmd);

        //scan directory for files of type .SHP
        $locationtoscan = "/var/www/html/uploads/$fn";
        $files = glob($locationtoscan.'/*.shp');

        
        //run cmd to load the first matching .shp in the director into the DB
  
        $cmd = 'ogr2ogr -f "PostgreSQL" PG:"dbname=nasafiremap user='.$username.' password='.$password.' " '. $files[0].' -nln user_'. $userid.' -skip-failures -overwrite';
        echo ("<br><br>".$cmd."<br>");

       echo ($cmd);

        echo shell_exec($cmd);
      }
      else
      {
        $filetoprocess = basename($target_file_save);
        $cmd = 'ogr2ogr -f "PostgreSQL" PG:"dbname=nasafiremap user='.$username.' password='.$password.' " /var/www/html/' . $target_file_save.' -nln user_'. $userid.' -skip-failures  -overwrite';
        echo ("<br><br>".$cmd."<br>");
        echo shell_exec($cmd);

      }

      
      echo ("<br><br>--- COPY POLYGONS to ALERT ZONES ---<br>");

      //copy the polygons to the monitorzones with the correct projectID  

      $u ='user_'.$userid;

      $link = pg_Connect("dbname=nasafiremap user=$username password=$password");

      if ($geoFileType=="gpkg") {
         $query = "insert into monitorzones (geom,projectid) select st_transform(geom,4326),$projectid from $u;";
      }
      else{      
        $query = "insert into monitorzones (geom,projectid) select st_transform(wkb_geometry,4326),$projectid from $u;";
      }
  
      $result = pg_exec($link,$query);
      pg_close($link);



      //delete the temp pg tables
      $link = pg_Connect("dbname=nasafiremap user=$username password=$password");
      $query = "drop table $u;";
      $result = pg_exec($link,$query);

      pg_close($link);


header("Location: projectlist_you.php");
die();
  


  }
  
  else {
    echo "Sorry, there was an error uploading your file.";
  }

}



?>
