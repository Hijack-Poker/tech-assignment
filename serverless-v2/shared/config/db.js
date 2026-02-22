'use strict';

const { Sequelize } = require('sequelize');

const sequelize = new Sequelize(
  process.env.MYSQL_DATABASE || 'hijack_poker',
  process.env.MYSQL_USER || 'hijack',
  process.env.MYSQL_PASSWORD || 'hijack_dev',
  {
    host: process.env.MYSQL_HOST || 'localhost',
    port: parseInt(process.env.MYSQL_PORT || '3306', 10),
    dialect: 'mysql',
    logging: false,
    pool: {
      max: 5,
      min: 0,
      idle: 10000,
      acquire: 30000,
    },
  }
);

module.exports = { sequelize };
