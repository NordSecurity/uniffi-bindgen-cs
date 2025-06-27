use serde::{Deserialize, Serialize};
use serde_with::{DeserializeFromStr, SerializeDisplay};

use smart_default::SmartDefault;

pub use ipnet::IpNet;

use base64::prelude::*;

use std::{
    fmt,
    net::{IpAddr, Ipv4Addr, SocketAddr},
};

use crate::features::PathType;

pub type FfiResult<T> = Result<T, TelioError>;

pub trait TelioEventCb: Send + Sync + std::fmt::Debug {
    fn event(&self, payload: Event) -> FfiResult<()>;
}

#[derive(Clone, Debug, Default, Serialize)]
#[serde(rename_all = "lowercase")]
pub enum ErrorLevel {
    #[default]
    Critical = 1,
    Severe = 2,
    Warning = 3,
    Notice = 4,
}

#[derive(Clone, Debug, Default, Serialize)]
#[serde(rename_all = "lowercase")]
pub enum ErrorCode {
    #[default]
    NoError = 0,
    Unknown = 1,
}

pub type EventMsg = String;

#[derive(Clone, Debug, Default, Serialize)]
pub struct ErrorEvent {
    pub level: ErrorLevel,
    pub code: ErrorCode,
    pub msg: EventMsg,
}

#[derive(Clone, Debug, Serialize)]
#[serde(tag = "type")]
#[serde(rename_all = "lowercase")]
pub enum Event {
    Relay { body: Server },
    Node { body: Node },
    Error { body: ErrorEvent },
}

#[derive(Debug, thiserror::Error)]
pub enum TelioError {
    #[error("UnknownError: {inner}")]
    UnknownError { inner: String },
    #[error("InvalidKey")]
    InvalidKey,
    #[error("BadConfig")]
    BadConfig,
    #[error("LockError")]
    LockError,
    #[error("InvalidString")]
    InvalidString,
    #[error("AlreadyStarted")]
    AlreadyStarted,
    #[error("NotStarted")]
    NotStarted,
}

#[derive(Debug, Default, Clone, Copy, PartialEq, Eq, Deserialize, Serialize)]
#[serde(rename_all = "lowercase")]
pub enum RelayState {
    #[default]
    Disconnected,
    Connecting,
    Connected,
}

pub const KEY_SIZE: usize = 32;

/// Error returned when parsing fails for SecretKey or PublicKey.
#[derive(Debug, thiserror::Error)]
pub enum KeyDecodeError {
    /// String was not of valid length for hex(64 bytes) or base64(44 bytes) parsing.
    #[error("Invalid length for parsing [{0}], 64-hex, 44-base64")]
    InvalidLength(usize),
    /// String was not valid for base64 decoding.
    #[error(transparent)]
    Base64(#[from] base64::DecodeSliceError),
    /// String was not valid for hex decoding.
    #[error(transparent)]
    Hex(#[from] hex::FromHexError),
}

#[derive(
    Default, PartialOrd, Ord, PartialEq, Eq, Hash, Copy, Clone, DeserializeFromStr, SerializeDisplay,
)]
pub struct PublicKey(pub [u8; KEY_SIZE]);

impl fmt::Display for PublicKey {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        let mut buf = [0u8; 44];
        let _ = BASE64_STANDARD.encode_slice(self.0, &mut buf);
        match std::str::from_utf8(&buf) {
            Ok(buf) => f.write_str(buf),
            Err(_) => Err(fmt::Error),
        }
    }
}

impl fmt::Debug for PublicKey {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        let buf = BASE64_STANDARD.encode(self.0);
        f.write_str(&format!(
            "\"{:.*}...{}\"",
            4,
            &buf,
            &buf.get((buf.len()) - 4..).ok_or(fmt::Error)?
        ))
    }
}

impl std::str::FromStr for PublicKey {
    type Err = KeyDecodeError;

    fn from_str(s: &str) -> Result<Self, Self::Err> {
        let mut key = [0; KEY_SIZE];

        match s.len() {
            64 => hex::decode_to_slice(s, &mut key)?,
            44 => {
                BASE64_STANDARD.decode_slice(s, &mut key)?;
            }
            l => return Err(KeyDecodeError::InvalidLength(l)),
        }

        Ok(Self::new(key))
    }
}

impl PublicKey {
    pub const fn new(bytes: [u8; 32]) -> Self {
        Self(bytes)
    }
}

#[derive(Debug, Clone, Deserialize, Serialize, SmartDefault)]
pub struct Server {
    pub region_code: String,
    pub name: String,
    pub hostname: String,
    #[default(Ipv4Addr::new(0, 0, 0, 0))]
    pub ipv4: Ipv4Addr,
    pub relay_port: u16,
    pub stun_port: u16,
    #[serde(default)]
    pub stun_plaintext_port: u16,
    pub public_key: PublicKey,
    pub weight: u32,
    #[serde(default)]
    pub use_plain_text: bool,
    #[serde(default)]
    pub conn_state: RelayState,
}

impl Server {
    pub fn get_address(&self) -> String {
        if self.use_plain_text {
            format!("http://{}:{}", self.hostname, self.relay_port)
        } else {
            format!("https://{}:{}", self.hostname, self.relay_port)
        }
    }
}

impl PartialEq for Server {
    fn eq(&self, other: &Self) -> bool {
        self.region_code == other.region_code
            && self.name == other.name
            && self.hostname == other.hostname
            && self.ipv4 == other.ipv4
            && self.relay_port == other.relay_port
            && self.stun_port == other.stun_port
            && self.stun_plaintext_port == other.stun_plaintext_port
            && self.public_key == other.public_key
            && self.use_plain_text == other.use_plain_text
    }
}

#[derive(Debug, Default, Clone, PartialEq, Eq, Deserialize, Serialize)]
pub struct Node {
    pub identifier: String,
    pub public_key: PublicKey,
    pub nickname: Option<String>,
    pub state: NodeState,
    pub link_state: Option<LinkState>,
    pub is_exit: bool,
    pub is_vpn: bool,
    pub ip_addresses: Vec<IpAddr>,
    pub allowed_ips: Vec<IpNet>,
    pub endpoint: Option<SocketAddr>,
    pub hostname: Option<String>,
    pub allow_incoming_connections: bool,
    pub allow_peer_traffic_routing: bool,
    pub allow_peer_local_network_access: bool,
    pub allow_peer_send_files: bool,
    pub path: PathType,
    pub allow_multicast: bool,
    pub peer_allows_multicast: bool,
}

#[derive(Debug, Default, Copy, Clone, PartialEq, Eq, Deserialize, Serialize)]
#[serde(rename_all = "lowercase")]
pub enum NodeState {
    #[default]
    Disconnected,
    Connecting,
    Connected,
}

#[derive(Copy, Clone, Debug, PartialEq, Eq, Deserialize, Serialize)]
#[serde(rename_all = "lowercase")]
pub enum LinkState {
    Down,
    Up,
}
