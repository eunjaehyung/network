<?php
	$host   = "localhost";
	$user   = "jjsprite";
	$pass   = "dkemals1";
	$dbName = "jjsprite";

	$connect = mysql_connect( $host, $user, $pass ) or die( "에러" );

	mysql_select_db( $dbName, $connect );
 
 	// http://jjsprite.dothome.co.kr/jjsprite.php?select=show
	if( $_REQUEST['select'] == "show" )
	{
		$sql = "select *from userinfo";
		$result = mysql_query( $sql,$connect ) or die( mysql_error() );
		while( $array = mysql_fetch_array( $result ) )
		{
			echo $array['id']."?";
			echo $array['name']."?";
			echo $array['pass']."&";
		}	
	}
	//　http://사이트/File.php?select=submit&id=Test&pass=1234&point=0
	//http://jjsprite.dothome.co.kr//jjsprite.php?select=submit&name=Test&pass=1234&point=123
	if( $_REQUEST['select'] == "submit" )
	{
		$valid 		= $_REQUEST['id'];
		$valName 	= $_REQUEST['name'];
		$valpass 	= $_REQUEST['pass'];
		$sql 		= "insert into userinfo(`id`, `name`, `pass`) values ('$valid', '$valName', '$valpass' );";
		$result 	= mysql_query( $sql,$connect ) or die( mysql_error() );
	}

	//http://사이트/File.php?select=ChangePoint&id=Test&point=100
	//http://jjsprite.dothome.co.kr/jjsprite.php?select=ChangePoint&id=eun&point=10000
	if( $_REQUEST['select'] == "ChangeUserInfo" )
	{
		$valid 		= $_REQUEST['id'];
		$valName 	= $_REQUEST['name'];
		$valpass 	= $_REQUEST['pass'];
		$sql 		= "update userinfo set name = '$valName', pass = '$valpass' where id = '$valid'";
		$result 	= mysql_query( $sql,$connect ) or die( mysql_error() );
		echo mysql_affected_rows();
	}
?>