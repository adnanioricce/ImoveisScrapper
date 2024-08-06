create table feature (
     id smallint unsigned PRIMARY KEY AUTO_INCREMENT,
     name varchar(64) NOT NULL,
     UNIQUE KEY (name)
);

create table property_feature (
      property_id int unsigned NOT NULL,
      feature_id smallint unsigned NOT NULL,
      PRIMARY KEY (property_id, feature_id)
);

CREATE TABLE properties(
	id SERIAL PRIMARY KEY
	, price DECIMAL(10,2)
	, title VARCHAR(512)
	, address VARCHAR(512)    
    , url NVARCHAR(MAX)    
);
CREATE TABLE images(
	id SERIAL PRIMARY KEY
	,property_id INT	
	,url NVARCHAR(MAX)
	
);