get-content dev.env | foreach {
    $name, $value = $_.split('=')
    set-content env:\$name $value
}