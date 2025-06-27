pub mod features;
pub mod types;

use base64::prelude::*;

use ipnet::IpNet;
use std::net::SocketAddr;
use std::net::{IpAddr, Ipv4Addr};

use crate::features::*;
use crate::types::*;

pub struct Telio {
    _id: usize,
}

impl Telio {
    pub fn new(features: Features, events: Box<dyn TelioEventCb>) -> FfiResult<Self> {
        println!("Constructor called!");
        // Call the callback with all variants
        events.event(Event::Error {
            body: ErrorEvent {
                level: ErrorLevel::Critical,
                code: ErrorCode::Unknown,
                msg: "Test error".to_string(),
            },
        })?;
        events.event(Event::Relay {
            body: Server {
                region_code: "Some region".to_string(),
                name: "Some name".to_string(),
                hostname: "Some hostname".to_string(),
                ipv4: Ipv4Addr::new(1, 2, 3, 4),
                relay_port: 80,
                stun_port: 81,
                stun_plaintext_port: 82,
                public_key: PublicKey::new([10; 32]),
                weight: 100,
                use_plain_text: false,
                conn_state: RelayState::Connected,
            },
        })?;
        events.event(Event::Node {
            body: Node {
                identifier: "Some identifier".to_string(),
                public_key: PublicKey::new([10; 32]),
                nickname: Some("Some nickname".to_string()),
                state: NodeState::Connected,
                link_state: Some(LinkState::Up),
                is_exit: false,
                is_vpn: false,
                ip_addresses: Vec::new(),
                allowed_ips: Vec::new(),
                endpoint: None,
                hostname: Some("Some hostname".to_string()),
                allow_incoming_connections: true,
                allow_peer_traffic_routing: true,
                allow_peer_local_network_access: true,
                allow_peer_send_files: true,
                path: PathType::Direct,
                allow_multicast: true,
                peer_allows_multicast: true,
            },
        })?;

        // Try to access the features struct
        if features.nicknames {
            do_nothing();
        }

        if features.wireguard.persistent_keepalive.direct == 5 {
            do_nothing();
        }

        if let Some(batching) = features.batching {
            if batching.direct_connection_threshold == 0 {
                do_nothing();
            }
        }
        Ok(Telio { _id: 123 })
    }
}

fn do_nothing() {}

fn decode_key(val: String) -> uniffi::Result<[u8; types::KEY_SIZE]> {
    let mut key = [0_u8; types::KEY_SIZE];
    let decoded_bytes = BASE64_STANDARD
        .decode(val)
        .map_err(|_| TelioError::InvalidKey)
        .and_then(|val| {
            if val.len() != types::KEY_SIZE {
                Err(TelioError::InvalidKey)
            } else {
                Ok(val)
            }
        })?;
    key.copy_from_slice(&decoded_bytes);
    Ok(key)
}

impl UniffiCustomTypeConverter for PublicKey {
    type Builtin = String;

    fn into_custom(val: Self::Builtin) -> uniffi::Result<Self> {
        let key = decode_key(val)?;
        Ok(PublicKey::new(key))
    }

    fn from_custom(obj: Self) -> Self::Builtin {
        BASE64_STANDARD.encode(obj.0)
    }
}

impl UniffiCustomTypeConverter for EndpointProviders {
    type Builtin = Vec<EndpointProvider>;

    fn into_custom(val: Self::Builtin) -> uniffi::Result<Self> {
        Ok(val.iter().copied().collect())
    }

    fn from_custom(obj: Self) -> Self::Builtin {
        obj.iter().copied().collect()
    }
}

impl UniffiCustomTypeConverter for IpAddr {
    type Builtin = String;

    fn into_custom(val: Self::Builtin) -> uniffi::Result<Self> {
        Ok(val.parse().map_err(|_| TelioError::UnknownError {
            inner: "Invalid IP address".to_owned(),
        })?)
    }

    fn from_custom(obj: Self) -> Self::Builtin {
        obj.to_string()
    }
}

impl UniffiCustomTypeConverter for Ipv4Addr {
    type Builtin = String;

    fn into_custom(val: Self::Builtin) -> uniffi::Result<Self> {
        val.parse()
            .map_err(|_| anyhow::anyhow!("Invalid IP address"))
    }

    fn from_custom(obj: Self) -> Self::Builtin {
        obj.to_string()
    }
}

impl UniffiCustomTypeConverter for SocketAddr {
    type Builtin = String;

    fn into_custom(val: Self::Builtin) -> uniffi::Result<Self> {
        Ok(val.parse().map_err(|_| TelioError::UnknownError {
            inner: "Invalid IP address".to_owned(),
        })?)
    }

    fn from_custom(obj: Self) -> Self::Builtin {
        obj.to_string()
    }
}

impl UniffiCustomTypeConverter for FeatureValidateKeys {
    type Builtin = bool;

    fn into_custom(val: Self::Builtin) -> uniffi::Result<Self> {
        Ok(FeatureValidateKeys(val))
    }

    fn from_custom(obj: Self) -> Self::Builtin {
        obj.0
    }
}

impl UniffiCustomTypeConverter for TtlValue {
    type Builtin = u32;

    fn into_custom(val: Self::Builtin) -> uniffi::Result<Self> {
        Ok(TtlValue(val))
    }

    fn from_custom(obj: Self) -> Self::Builtin {
        obj.0
    }
}

impl UniffiCustomTypeConverter for IpNet {
    type Builtin = String;

    fn into_custom(val: Self::Builtin) -> uniffi::Result<Self> {
        Ok(val.parse().map_err(|_| TelioError::UnknownError {
            inner: "Invalid IP address".to_owned(),
        })?)
    }

    fn from_custom(obj: Self) -> Self::Builtin {
        obj.to_string()
    }
}

impl UniffiCustomTypeConverter for features::Ipv4Net {
    type Builtin = String;

    fn into_custom(val: Self::Builtin) -> uniffi::Result<Self> {
        val.parse()
            .map_err(|_| anyhow::anyhow!("Invalid IP address".to_owned()))
    }

    fn from_custom(obj: Self) -> Self::Builtin {
        obj.to_string()
    }
}

uniffi::include_scaffolding!("libtelio");
