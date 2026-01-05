'use strict';

const cloneDeep = require('clone-deep');

/**
 * 각 항목은 부여된 가중치 만큼의 확률을 가진다.
 * ex) 입력된 항목이 다음과 같을 때,
 *     {base: 10, value: A}, {base: 20, value: B}, {base: 30, value: C}, {base: 40, value: D}
 *     전체 가중치는 100 (10+20+30+40) 이고,
 *     각 항목의 추출 확률은 아래와 같다.
 *     A: 10/100, B: 20/100, C: 30/100, D: 40/100
 */
class WeightedRandom {

    constructor() {

        this._table = [];
        this._weight = 0;
        this._latestPos = 0;
    }

    get length() {

        return this._table.length;
    }

    push(weight, value) {

        if (weight > 0) {
            this._table.push({ base: this._weight, value, weight });
            this._weight += weight;
        }
    }

    slice() {

        const obj = new WeightedRandom();
        obj._weight = this._weight;
        obj._table = cloneDeep(this._table);
        return obj;
    }

    _random() {

        const table = this._table;
        const weight = Math.floor(Math.random() * this._weight);
        let low = -1;
        let hi = table.length;

        while (hi - low > 1) {
            const mid = Math.round((low + hi) / 2);
            if (table[mid].base <= weight) {
                low = mid;
            } else {
                hi = mid;
            }
        }

        this._latestPos = low;
        return low;
    }

    random() {

        return this._table[this._random()].value;
    }

    next(step) {

        if (!step) {
            this._latestPos = Math.floor(Math.random() * this._table.length);
        }
        else {
            this._latestPos++;
        }

        if (this._table.length <= this._latestPos) {
            this._latestPos = 0;
        }

        return this._table[this._latestPos].value;
    }

    weight() { return this._weight; }

    extractor() { return new WeightedExtractor(this); }

    forEach(cb) { this._table.forEach(cb); }
}


class WeightedExtractor {

    constructor (weightedRandom) {

        this._random = weightedRandom.slice();
    }

    get length() {

        return this._random._table.length;
    }

    exclude (values, comp = (a, b) => a === b) {

        if (!values) {
            values = [];
        }
        else if (!Array.isArray(values)) {
            values = [values];
        }

        values.forEach((value) => {

            const table = this._random._table;
            const length = table.length;
            for(let index = 0; index < length; index++) {
                if (comp(table[index].value, value)) {
                    this._pop(index);
                    return;
                }
            }
        });
    }

    random() {

        const index = this._random._random();
        const result = this._random._table[index];
        this._pop(index);
        return result.value;
    }

    _pop(index) {

        const table = this._random._table;
        let weight = table[index].base;
        for (; index + 1 < table.length; index++) {
            const item = table[index] = table[index + 1];
            item.base = weight;
            weight += item.weight;
        }

        table.pop();
        this._random._weight = weight;
    }
}

/**
 *
 */
module.exports = WeightedRandom;
