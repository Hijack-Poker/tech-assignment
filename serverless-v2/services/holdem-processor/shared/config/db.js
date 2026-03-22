'use strict';

const { Sequelize } = require('sequelize');

// Support both MYSQL_HOST (Docker/custom) and MYSQLHOST (Railway) env var formats
const sequelize = process.env.MYSQL_URL || process.env.MYSQURL
  ? new Sequelize(process.env.MYSQL_URL || process.env.MYSQURL, {
      dialect: 'mysql',
      logging: false,
      pool: { max: 5, min: 0, idle: 10000, acquire: 30000 },
    })
  : new Sequelize(
      process.env.MYSQL_DATABASE || process.env.MYSQLDATABASE || 'hijack_poker',
      process.env.MYSQL_USER || process.env.MYSQLUSER || 'hijack',
      process.env.MYSQL_PASSWORD || process.env.MYSQLPASSWORD || 'hijack_dev',
      {
        host: process.env.MYSQL_HOST || process.env.MYSQLHOST || 'localhost',
        port: parseInt(process.env.MYSQL_PORT || process.env.MYSQLPORT || '3306', 10),
        dialect: 'mysql',
        logging: false,
        pool: { max: 5, min: 0, idle: 10000, acquire: 30000 },
      }
    );

module.exports = { sequelize };
