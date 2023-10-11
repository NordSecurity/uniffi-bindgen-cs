/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

use once_cell::sync::Lazy;
use std::collections::HashMap;
use std::sync::{Arc, RwLock};

static LIVE_COUNT: Lazy<RwLock<i32>> = Lazy::new(|| RwLock::new(0));

#[derive(Debug, Clone)]
pub struct Resource {}

impl Resource {
    pub fn new() -> Self {
        *LIVE_COUNT.write().unwrap() += 1;
        Resource {}
    }
}

impl Drop for Resource {
    fn drop(&mut self) {
        *LIVE_COUNT.write().unwrap() -= 1;
    }
}

#[derive(Debug, Clone)]
pub struct ResourceJournalList {
    resources: Vec<Arc<Resource>>,
}

#[derive(Debug, Clone)]
pub struct ResourceJournalMap {
    resources: HashMap<i32, Arc<Resource>>,
}

#[derive(Debug, Clone)]
pub struct ResourceJournalMapList {
    resources: HashMap<i32, Option<Vec<Arc<Resource>>>>,
}

pub enum MaybeResourceJournal {
    Some { resource: ResourceJournalList },
    None,
}

fn get_live_count() -> i32 {
    *LIVE_COUNT.read().unwrap()
}

fn get_resource() -> Arc<Resource> {
    Arc::new(Resource::new())
}

fn get_resource_journal_list() -> ResourceJournalList {
    ResourceJournalList {
        resources: vec![get_resource(), get_resource()],
    }
}

fn get_resource_journal_map() -> ResourceJournalMap {
    ResourceJournalMap {
        resources: HashMap::from([(1, get_resource()), (2, get_resource())]),
    }
}

fn get_resource_journal_map_list() -> ResourceJournalMapList {
    ResourceJournalMapList {
        resources: HashMap::from([(1, Some(vec![get_resource(), get_resource()])), (2, None)]),
    }
}

fn get_maybe_resource_journal() -> MaybeResourceJournal {
    MaybeResourceJournal::Some {
        resource: get_resource_journal_list(),
    }
}

uniffi::include_scaffolding!("disposable");
