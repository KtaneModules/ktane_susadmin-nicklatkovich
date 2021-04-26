const SECURITY_PROTOCOLS_COUNT = 6;

const viruses = new Set();
const data = new Array(SECURITY_PROTOCOLS_COUNT).fill(0).map(() => new Array(SECURITY_PROTOCOLS_COUNT).fill(0).map(() => {
	let result;
	do {
		result = new Array(4).fill(0).map(() => (
			Math.random() < .5 ? Math.floor(Math.random() * 10).toString() : String.fromCharCode(Math.floor(Math.random() * 26) + "A".charCodeAt(0))
		)).join("");
	} while (viruses.has(result));
	viruses.add(result);
	return result;
}));

console.log(data);
for (const virus of viruses) console.log(virus, Math.floor(Math.random() * 100), Math.floor(Math.random() * 100));
