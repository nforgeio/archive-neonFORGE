#!/bin/bash
#------------------------------------------------------------------------------
# This script generates the [/etc/td-agent/template/logstash-template.json] file.
#
# These variables MUST be defined.  [docker-entrypoint.sh] takes care of this.

mkdir -p /etc/td-agent/template
chmod 600 /etc/td-agent
chmod 600 /etc/td-agent/template

cat <<EOF > /etc/td-agent/template/logstash-template.json
{
  "template": "logstash-*",
  "index_patterns": [ "logstash-*" ],
  "settings": {
    "refresh_interval": "1s"
  },
  "mappings": {
    "default": {
      "_meta": {
        "version": "0.0.1"
      },
      "dynamic_templates": [
        {
          "strings_as_keyword": {
            "mapping": {
              "ignore_above": 1024,
              "type": "keyword"
            },
            "match_mapping_type": "string"
          }
        }
      ],
      "_all": {
        "enabled": false
      },
      "_source": {
        "enabled": true
      },
      "properties": {
        "@timestamp": {
          "type": "date",
          "format": "strict_date_optional_time||epoch_millis"
        },
        "index": {
          "type": "long"
        },
        "activity_id": {
          "type": "keyword"
        },
        "cluster": {
          "type": "keyword"
        },
        "cid": {
          "type": "keyword"
        },
        "cid_full": {
          "type": "keyword"
        },
        "datacenter": {
          "type": "keyword"
        },
        "environment": {
          "type": "keyword"
        },
        "json": {
          "type": "text"
        },
        "level": {
          "type": "keyword"
        },
        "location": {
          "properties": {
            "latitude": {
              "type": "scaled_float",
              "scaling_factor": 100000.0
            },
            "longitude": {
              "type": "scaled_float",
              "scaling_factor": 100000.0
            },
            "metro_code": {
              "type": "integer"
            },
            "postal_code": {
              "type": "keyword"
            },
            "time_zone": {
              "type": "keyword"
            },
            "continent": {
              "properties": {
                "code": {
                  "type": "keyword"
                },
                "name": {
                  "type": "keyword"
                }
              }
            },
            "country": {
              "properties": {
                "code": {
                  "type": "keyword"
                },
                "name": {
                  "type": "keyword"
                }
              }
            },
            "county": {
              "properties": {
                "code": {
                  "type": "keyword"
                },
                "name": {
                  "type": "keyword"
                }
              }
            },
            "state": {
              "properties": {
                "code": {
                  "type": "keyword"
                },
                "name": {
                  "type": "keyword"
                }
              }
            },
            "city": {
              "properties": {
                "name": {
                  "type": "keyword"
                }
              }
            }
          }
        },
        "message": {
          "type": "text"
        },
        "module": {
          "type": "keyword"
        },
        "node": {
          "type": "keyword"
        },
        "node_dnsname": {
          "type": "keyword"
        },
        "node_ip": {
          "type": "ip"
        },
        "node_role": {
          "type": "keyword"
        },
        "num": {
          "properties": {
            "0": {
              "type": "double"
            },
            "1": {
              "type": "double"
            },
            "2": {
              "type": "double"
            },
            "3": {
              "type": "double"
            },
            "4": {
              "type": "double"
            },
            "5": {
              "type": "double"
            },
            "6": {
              "type": "double"
            },
            "7": {
              "type": "double"
            },
            "8": {
              "type": "double"
            },
            "9": {
              "type": "double"
            }
          }
        },
        "service": {
          "type": "keyword"
        },
        "service_host": {
          "type": "keyword"
        },
        "service_type": {
          "type": "keyword"
        },
        "tag": {
          "type": "keyword"
        },
        "txt": {
          "properties": {
            "0": {
              "type": "keyword"
            },
            "1": {
              "type": "keyword"
            },
            "2": {
              "type": "keyword"
            },
            "3": {
              "type": "keyword"
            },
            "4": {
              "type": "keyword"
            },
            "5": {
              "type": "keyword"
            },
            "6": {
              "type": "keyword"
            },
            "7": {
              "type": "keyword"
            },
            "8": {
              "type": "keyword"
            },
            "9": {
              "type": "keyword"
            }
          }
        },
        "proxy": {
          "properties": {
            "browser": {
              "properties": {
                "bot": {
                  "type": "boolean"
                },
                "device": {
                  "type": "keyword"
                },
                "name": {
                  "type": "keyword"
                },
                "os_name": {
                  "type": "keyword"
                },
                "os_version": {
                  "type": "keyword"
                },
                "version": {
                  "type": "keyword"
                }
              }
            },
            "bytes_received": {
              "type": "long"
            },
            "bytes_sent": {
              "type": "long"
            },
            "cdn": {
              "type": "keyword"
            },
            "client_ip": {
              "type": "ip"
            },
            "conn_proxy": {
              "type": "integer"
            },
            "conn_frontend": {
              "type": "integer"
            },
            "conn_backend": {
              "type": "integer"
            },
            "conn_server": {
              "type": "integer"
            },
            "mode": {
              "type": "keyword"
            },
            "queue_server": {
              "type": "integer"
            },
            "queue_backend": {
              "type": "integer"
            },
            "retries": {
              "type": "short"
            },
            "route": {
              "type": "keyword"
            },
            "server": {
              "type": "keyword"
            },
            "server_ip": {
              "type": "ip"
            },
            "server_port": {
              "type": "integer"
            },
            "time_queue": {
              "type": "scaled_float",
              "scaling_factor": 1000.0
            },
            "time_connect": {
              "type": "scaled_float",
              "scaling_factor": 1000.0
            },
            "time_session": {
              "type": "scaled_float",
              "scaling_factor": 1000.0
            },
            "termination": {
              "type": "keyword"
            },
            "tls_version": {
              "type": "keyword"
            },
            "tls_cypher": {
              "type": "keyword"
            },
            "http_host": {
              "type": "keyword"
            },
            "http_method": {
              "type": "keyword"
            },
            "http_status": {
              "type": "short"
            },
            "http_time_active": {
              "type": "scaled_float",
              "scaling_factor": 1000.0
            },
            "http_time_idle": {
              "type": "scaled_float",
              "scaling_factor": 1000.0
            },
            "http_time_request": {
              "type": "scaled_float",
              "scaling_factor": 1000.0
            },
            "http_time_response": {
              "type": "scaled_float",
              "scaling_factor": 1000.0
            },
            "http_uri": {
              "type": "keyword"
            },
            "http_uri_query": {
              "type": "keyword"
            },
            "http_user_agent": {
              "type": "text"
            },
            "http_version": {
              "type": "keyword"
            }
          }
        }
      }
    }
  }
}
EOF

chmod 600 /etc/td-agent/template/logstash-template.json
